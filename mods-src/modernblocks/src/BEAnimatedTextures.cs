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
    class BEAnimatedTextures:BlockEntity
    {
        TestRenderer testRenderer;
        static Random r;
        
        ICoreClientAPI capi;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (r == null) { r = new Random(); }
            if (api is ICoreClientAPI)
            {

                capi = api as ICoreClientAPI;
                
                RegisterGameTickListener(Update, 200);
                
            }
        }
        void Update(float df)
        {
            List<BlockFacing> neighbors = new List<BlockFacing>(); //determines which faces to render
            List<BlockFacing> matchneighbors = new List<BlockFacing>(); //determines which faces to connect
            foreach (BlockFacing bf in BlockFacing.ALLFACES)
            {
                Block nblock = Api.World.BlockAccessor.GetBlock(Pos.Copy().Offset(bf));
                if (nblock == null || nblock.Id == 0) { continue; }
                if (nblock.Id == this.Block.Id) { matchneighbors.Add(bf); } //TODO Add a check for texture or something
                if (!nblock.SideOpaque[bf.Opposite.Index] && !nblock.AllSidesOpaque) { continue; }
                neighbors.Add(bf);

            }
            if (neighbors.Count() == 6) { return; } //if neighbours on all sides we don't need to do any rendering

            if (testRenderer == null) { testRenderer = new TestRenderer(Pos, capi); }
            testRenderer.TextureName = new AssetLocation("modernblocks:block/connectedtextures/weirdcomputer.png");

            testRenderer.facedata = new List<FaceData>();

            foreach (BlockFacing bf in BlockFacing.ALLFACES)
            {
                if (neighbors.Contains(bf)) { continue; }
                FaceData fd = new FaceData(bf);
                /*if (bf == BlockFacing.DOWN || bf == BlockFacing.UP||bf==BlockFacing.SOUTH)
                {
                    fd.vcell = 1;fd.ucell = 1;
                }
                else if (bf == BlockFacing.EAST || bf == BlockFacing.WEST)
                {
                    fd.vcell = 1;fd.ucell = 0;
                }
                else
                {*/
                    float d = Pos.DistanceTo(capi.World.Player.Entity.Pos.AsBlockPos);
                    if (d < 3) { fd.SetCells(r.Next(7, 9)); }
                    else if (d < 7) { fd.SetCells(r.Next(0, 4)); }
                    else { fd.vcell = 1;fd.ucell = 2; }
                //}
                testRenderer.facedata.Add(fd);
                testRenderer.GenModel();
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
        public override void OnBlockRemoved()
        {
            if (capi == null) { base.OnBlockRemoved(); return; }
            testRenderer?.Dispose();
            base.OnBlockRemoved();
        }
    }
}
