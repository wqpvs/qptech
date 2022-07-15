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
    class BEConnectedTexturesWithRenderer : BlockEntity
    {
    
        
        ICoreClientAPI capi;
        List<BlockFacing> oldneighbors;
        ConnectedTextureRenderer testRenderer;
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
                if (nblock.Id == this.Block.Id) { matchneighbors.Add(bf); } //TODO Add a check for texture or something
                if (!nblock.SideOpaque[bf.Opposite.Index]&&!nblock.AllSidesOpaque) { continue; }
                neighbors.Add(bf);
                
            }
            if (neighbors.Count() == 6) { return; } //if neighbours on all sides we don't need to do any rendering
            if (oldneighbors!=null&& neighbors.Equals(oldneighbors)) { return; }
            testRenderer = new ConnectedTextureRenderer(Pos, capi);
            testRenderer.TextureName = new AssetLocation("modernblocks:block/connectedtextures/cultictree.png");
            
            testRenderer.facedata = new List<FaceData>();
            
            foreach (BlockFacing bf in BlockFacing.ALLFACES)
            {
                if (neighbors.Contains(bf)) { continue; }
                FaceData fd = new FaceData(bf);
                fd.SetConnectedTextures(matchneighbors.ToArray());
                //fd.rgba = new byte[] { (byte)r.Next(0, 256), (byte)r.Next(0, 256), (byte)r.Next(0, 256), 255 };
                //fd.rgba = new byte[] { 128, 128, 128, 255 };
                testRenderer.facedata.Add(fd);
                testRenderer.GenModel();
                oldneighbors = new List<BlockFacing>(neighbors);
            }
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