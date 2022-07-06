using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.API.Config;

namespace qptech.src.procanim
{
    class BEBProceduralAnimator:BlockEntityBehavior
    {
        ICoreClientAPI capi;
        BlockEntity blockEntity;
        public BEBProceduralAnimator(BlockEntity be) : base(be)
        {
            blockEntity = be;
        }
        Block partblock;
        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            if (api is ICoreClientAPI)
            {
                capi = api as ICoreClientAPI;
                blockEntity.RegisterGameTickListener(OnClientTick, 25);
                
            }
        }
        public void OnClientTick(float df)
        {
            GenMesh();
            blockEntity.MarkDirty(true);
        }
        MeshData meshdata;
        float xrot = 0;
        float xrotdir = -1;
        float xrotspeed = 5;
        void GenMesh()
        {
            AssetLocation al = new AssetLocation("machines:cuttingmachineblade");
            Block partblock = capi.World.GetBlock(al);
            if (partblock == null) {
                return;
            }
            capi.Tesselator.TesselateBlock(partblock, out meshdata);
            meshdata.Rotate(new Vec3f(0.5f, 0.45f, 0.14f), GameMath.DEG2RAD*xrot, 0, 0);
            xrot += xrotdir*xrotspeed;
            if (xrot > 90) { xrot = 90;xrotdir = -1; }
            else if (xrot <0) { xrot = 0;xrotdir = 1; }
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            try { mesher.AddMeshData(meshdata); }
            catch { return base.OnTesselation(mesher, tessThreadTesselator); }

            return base.OnTesselation(mesher, tessThreadTesselator);
            
        }
    }
}
