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
        
        
        
        const int n_ = 1;
        const int e_ = 2;
        const int s_ = 4;
        const int w_ = 8;
        
        

        public Size2i AtlasSize => (Api as ICoreClientAPI).BlockTextureAtlas.Size;
        ICoreClientAPI capi;
        //string basetexturename = "block/stone/concretetile/concretetile-";

        //string fulltexturename => basetexturename + suffix[directionmap];

        
        Dictionary<BlockFacing, bool> oldneighbors;
        TestRenderer testRenderer;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            
            if (api is ICoreClientAPI )
            {

                capi = api as ICoreClientAPI;
                UpdateRenderer();
            }
        }

        public virtual void UpdateRenderer()
        {
            if (capi == null) { return; }
            testRenderer?.Dispose();
            
            List<BlockFacing> neighbors = new List<BlockFacing>();
            foreach (BlockFacing bf in BlockFacing.ALLFACES)
            {
                BlockEntity nblock = Api.World.BlockAccessor.GetBlockEntity(Pos.Copy().Offset(bf));
                if (nblock == null) { continue; }
                neighbors.Add(bf);
            }
            if (neighbors.Count() == 6) { return; } //if neighbours on all sides we don't need to do any rendering

            capi.Event.RegisterRenderer(testRenderer = new TestRenderer(Pos, capi), EnumRenderStage.Opaque, "test");
            testRenderer.TextureName = new AssetLocation("modernblocks:block/connectedtextures/testgrid.png");
            testRenderer.GenModel();
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