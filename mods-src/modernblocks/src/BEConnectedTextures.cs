using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace modernblocks.src
{
    /// <summary>
    /// 
    /// This version will create the textures using ontesslation instead of a renderer:
    /// idea only update the textures when block placed (send the update command to 
    /// 3x3x3 blocks when a new ct block is placed)
    /// Also maybe cache generated meshes somehow, so if a block has the same setup it could jsut use a pregenerated mesh?
    /// 
    /// 
    /// Also store last setup somehow so when the chunk loads it'll just use what it had before
    /// 
    /// Render a texture so the outer borders change automatically to have closed borders
    /// Think we will do larger textures and then do appropriate UVs
    /// So a 4x4 texture top 3x3 would be the outer ring, and 0x3 would be fully closed texture
    /// (leaves 1x3,2x3,and 3x3 slots unused - maybe could do alternates or some other expansion?
    /// Also would like:
    /// user setable textures from a palette of graphics (like that MC chisel mod)
    /// could maybe also sub in any texture if we can get the proper file reference (risks crashing though), would have to adjust uvs
    /// 
    /// </summary>
    class BEConnectedTextures : BlockEntity
    {
    
        
        ICoreClientAPI capi;
        List<BlockFacing> oldneighbors;
        
        static Random r;
        //MeshRef cubeModelRef;
        public List<FaceData> facedata;
        public AssetLocation TextureName = null;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (r == null) { r = new Random(); }
            if (api is ICoreClientAPI )
            {

                capi = api as ICoreClientAPI;
                //RegisterDelayedCallback(DelayedStart,r.Next(1,20)); //hopefully lets world load and prevents all blocks updating at once
            }
        }
        //void DelayedStart(float dt)
        //{
        //    FindNeighbours();
       // }
        public virtual void FindNeighbours()
        {
            if (capi == null) { return; }
            
            
            List<BlockFacing> neighbors = new List<BlockFacing>(); //determines which faces to render
            List<BlockFacing> matchneighbors = new List<BlockFacing>(); //determines which faces to connect
            foreach (BlockFacing bf in BlockFacing.ALLFACES)
            {
                Block nblock = Api.World.BlockAccessor.GetBlock(Pos.Copy().Offset(bf));
                if (nblock == null||nblock.Id==0) { continue; }
                if (nblock.Id == this.Block.Id) { matchneighbors.Add(bf); } //TODO Add a check for texture or something
                if (!nblock.SideOpaque[bf.Opposite.Index]&&!nblock.AllSidesOpaque) { continue; }
                neighbors.Add(bf);
                
            }
            if (neighbors.Count() == 6) { return; } //if neighbours on all sides we don't need to do any rendering
            if (oldneighbors!=null&& neighbors.Equals(oldneighbors)) { return; }
            
            TextureName = new AssetLocation("modernblocks:block/connectedtextures/cultictree.png");
            
            facedata = new List<FaceData>();
            
            foreach (BlockFacing bf in BlockFacing.ALLFACES)
            {
                if (neighbors.Contains(bf)) { continue; }
                FaceData fd = new FaceData(bf);
                fd.SetConnectedTextures(matchneighbors.ToArray());
                //fd.rgba = new byte[] { (byte)r.Next(0, 256), (byte)r.Next(0, 256), (byte)r.Next(0, 256), 255 };
                fd.rgba = new byte[] { 128, 128, 128, 255 };
                facedata.Add(fd);
                
                oldneighbors = new List<BlockFacing>(neighbors);
            }
            GenModel();
        }
        int texid = 0;
        //these define the points on the various cube faces
        public static readonly Dictionary<BlockFacing, List<float>> cubeVertexLookup = new Dictionary<BlockFacing, List<float>>() {
            { BlockFacing.WEST,new List<float>() { 0, 0, 0, 0, 0, 1, 0, 1, 0, 0, 1, 1 } },
            { BlockFacing.NORTH,new List<float>() { 0, 0, 0, 0, 1, 0, 1, 0, 0, 1, 1, 0, } },
            { BlockFacing.EAST,new List<float>() { 1, 0, 0, 1, 0, 1, 1, 1, 0, 1, 1, 1 } },
            { BlockFacing.SOUTH,new List<float>() { 0, 0, 1, 0, 1, 1, 1, 0, 1, 1, 1, 1, } },
            { BlockFacing.UP,new List<float>() { 0, 1, 0, 0, 1, 1, 1, 1, 0, 1, 1, 1, } },
            { BlockFacing.DOWN,new List<float>() { 0, 0, 0, 0, 0, 1, 1, 0, 0, 1, 0, 1 } }
        };
        MeshData m;
        public void GenModel()
        {
            //cubeModelRef?.Dispose();
            
            if (facedata == null || facedata.Count == 0) { return; }
            texid = capi.Render.GetOrLoadTexture(TextureName);


            m = new MeshData();

            capi.Tesselator.TesselateBlock(Block, out m);
            float[] uvmorpher = m.Uv;
            //find UV origin and size
           
            float ustart = 1;
            float uend = 0;
            float vstart =1;
            float vend = 0;
            for (int c = 0; c < m.Uv.Length; c++)
            {
                float thisuv = m.Uv[c];
                if (c % 2 == 0)//this is even so it's a V
                {
                    if (thisuv > vend) { vend = thisuv; }
                    if (thisuv < vstart) { vstart = thisuv; }
                }
                else
                {
                    if (thisuv > uend) { uend = thisuv; }
                    if (thisuv < ustart) { ustart = thisuv; }
                }
            }
            float uvsize = (uend - ustart) / 4;

            for (int c = 0; c < m.Uv.Length; c++)
            {
                float uvget = m.Uv[c];
                if (c % 2 == 0) //V
                {
                    if (uvget == vend)
                    {
                        uvget = vstart + uvsize ;
                    }
                }
                else //U
                {
                    if (uvget == uend)
                    {
                        uvget = ustart + uvsize ;
                    }
                }
                m.Uv[c] = uvget;
            }
            //cubeModelRef = capi.Render.UploadMesh(m);
        }
        public float[] ModelMatf = Mat4f.Create();
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            bool oktorender = true;
            if (m==null) {
               
                if (m == null)
                {
                    oktorender = false;
                }
            }
            
            if (!oktorender) { return base.OnTesselation(mesher, tessThreadTesselator); }
            mesher.AddMeshData(m);
            
            return true;
        }


        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);

        }

        public override void OnBlockRemoved()
        {
            if (capi == null) { base.OnBlockRemoved();return; }
            
            base.OnBlockRemoved();
        }
    }

    
}