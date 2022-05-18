using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.API.Util;

namespace modernblocks.src
{
    /// <summary>
    /// Render a texture so the outer borders change automatically to have closed borders
    /// Think we will do larger textures and then do appropriate UVs
    /// So a 4x4 texture top 3x3 would be the outer ring, and 0x3 would be fully closed texture
    /// (leaves 1x3,2x3,and 3x3 slots unused - maybe could do alternates or some other expansion?
    /// Also would like:
    /// user setable textures from a palette of graphics (like that MC chisel mod)
    /// could maybe also sub in any texture if we can get the proper file reference (risks crashing though), would have to adjust uvs
    /// Also could probably hav
    /// </summary>
    class BEConnectedTextures : BlockEntity
    {
        
        
        
        
        
        

        public Size2i AtlasSize => (Api as ICoreClientAPI).BlockTextureAtlas.Size;
        ICoreClientAPI capi;
        //string basetexturename = "block/stone/concretetile/concretetile-";

        //string fulltexturename => basetexturename + suffix[directionmap];

        
        List<BlockFacing> oldneighbors;
        TestRenderer testRenderer;
        static Random r;
        
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (r == null) { r = new Random(); }
            if (api is ICoreClientAPI )
            {

                capi = api as ICoreClientAPI;
                RegisterDelayedCallback(DelayedStart,r.Next(1,20)); //hopefully lets world load and prevents all blocks updating at once
            }
        }
        void DelayedStart(float dt)
        {
            UpdateRenderer();
        }
        public virtual void UpdateRenderer()
        {
            if (capi == null) { return; }
            testRenderer?.Dispose();
            
            List<BlockFacing> neighbors = new List<BlockFacing>(); //determines which faces to render
            List<BlockFacing> matchneighbors = new List<BlockFacing>(); //determines which faces to connect
            foreach (BlockFacing bf in BlockFacing.ALLFACES)
            {
                Block nblock = Api.World.BlockAccessor.GetBlock(Pos.Copy().Offset(bf));
                if (nblock == null||nblock.Id==0) { continue; }
                neighbors.Add(bf);
                BEConnectedTextures bect = Api.World.BlockAccessor.GetBlockEntity(Pos.Copy().Offset(bf)) as BEConnectedTextures;
                if (bect!=null) { matchneighbors.Add(bf); } //TODO Add a check for texture or something
                
            }
            if (neighbors.Count() == 6) { return; } //if neighbours on all sides we don't need to do any rendering
            if (oldneighbors!=null&& neighbors.Equals(oldneighbors)) { return; }
            capi.Event.RegisterRenderer(testRenderer = new TestRenderer(Pos, capi), EnumRenderStage.Opaque, "test");
            testRenderer.TextureName = new AssetLocation("modernblocks:block/connectedtextures/testgrid.png");
            testRenderer.facedata = new List<FaceData>();
            BlockFacing testbf = BlockFacing.UP;
            //foreach (BlockFacing bf in BlockFacing.ALLFACES)
            //{
                FaceData fd = new FaceData(testbf);
                fd.SetConnectedTextures(matchneighbors.ToArray());
                // Random colors: fd.rgba = new byte[] { (byte)r.Next(0, 256), (byte)r.Next(0, 256), (byte)r.Next(0, 256), 255 };
                // Random cells: fd.ucell = r.Next(0, 4);
                //               fd.vcell = r.Next(0, 4);
                testRenderer.facedata.Add(fd);
                testRenderer.GenModel();
                oldneighbors = new List<BlockFacing>(neighbors);
            //}
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {

            /*if (meshdata == null) { return base.OnTesselation(mesher, tessThreadTesselator); }
            mesher.AddMeshData(meshdata);
            try
            {
                meshref = capi.Render.UploadMesh(meshdata);
            }
            catch
            {

            }*/
            return true;
        }


        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);

        }

        public override void OnBlockRemoved()
        {
            if (capi == null) { base.OnBlockRemoved();return; }
            testRenderer?.Dispose();
            base.OnBlockRemoved();
        }
    }

    
}