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

            List<float> cubeVertices = new List<float>();
            List<int> cubeindices = new List<int>();
            List<float> cubeUVs = new List<float>();
            int vc = 0;

            foreach (FaceData fd in facedata)
            {
                BlockFacing tbf = fd.facing;

                float u1 = fd.ucell * fd.cellsize;
                float u2 = u1 + fd.cellsize;
                float v1 = fd.vcell * fd.cellsize;
                float v2 = v1 + fd.cellsize;
                cubeVertices.AddRange(cubeVertexLookup[tbf]);
                cubeindices.AddRange(new List<int>() { 3 + vc * 4, 1 + vc * 4, 0 + vc * 4, 2 + vc * 4, 0 + vc * 4, 3 + vc * 4 });
                vc++;
                if (tbf == BlockFacing.WEST)
                {
                    cubeUVs.AddRange(new List<float>() {u1 ,v2 ,
                u2 ,v2 ,
                u1 ,v1 ,
                u2 ,v1 });
                }
                else if (tbf == BlockFacing.NORTH)
                {
                    cubeUVs.AddRange(new List<float>() { u2 ,v2 ,
                u2 ,v1 ,
                u1 ,v2 ,
                u1 ,v1});
                }
                else if (tbf == BlockFacing.EAST)
                {
                    cubeUVs.AddRange(new List<float>() { u2 ,v2 ,
                u1 ,v2 ,
                u2 ,v1 ,
                u1 ,v1});
                }
                else if (tbf == BlockFacing.SOUTH)
                {
                    cubeUVs.AddRange(new List<float>() {u1 ,v2 ,
                u1, v1 ,
                u2 ,v2,
                u2 ,v1 });
                }
                else if (tbf == BlockFacing.UP)
                {
                    cubeUVs.AddRange(new List<float>() { u1, v1 ,
                u1 ,v2 ,
                u2 ,v1 ,
                u2 ,v2 });
                }
                else if (tbf == BlockFacing.DOWN)
                {
                    cubeUVs.AddRange(new List<float>() {u1, v2 ,
                u1 ,v1 ,
                u2 ,v2 ,
                u2 ,v1 });
                }
            }
            int numVerts = cubeVertices.Count / 3;
            m = new MeshData();

            //XYZ these are the vertices on our cube
            float[] xyz = new float[cubeVertices.Count];
            for (int i = 0; i < cubeVertices.Count; i++)
            {
                xyz[i] = cubeVertices[i];
            }
            m.SetXyz(xyz);

            //Set the UV coordinates for the triangles
            float[] uv = new float[cubeUVs.Count];
            for (int i = 0; i < uv.Length; i++)
            {
                uv[i] = cubeUVs[i];
            }
            m.SetUv(uv);
            m.SetVerticesCount(cubeVertices.Count);
            m.SetIndices(cubeindices.ToArray());
            m.SetIndicesCount(cubeindices.Count);


            m.Rgba = new byte[numVerts * 4]; //colored vertex shading

            FaceData usedata = facedata[0];
            for (int cc = 0; cc < numVerts * 4; cc += 4)
            {

                m.Rgba[cc] = usedata.rgba[0];
                m.Rgba[cc + 1] = usedata.rgba[1];
                m.Rgba[cc + 2] = usedata.rgba[2];
                m.Rgba[cc + 3] = usedata.rgba[3];
            }


            m.Flags = new int[numVerts * 4]; //not clear on what flags do
            ITexPositionSource tps = capi.Tesselator.GetTexSource(this.Block);
           
            m.SetTexPos(tps["n"]);
            capi.Tesselator.TesselateBlock(Block, out m);
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