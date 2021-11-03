using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace qptech.src
{
    class BEPowerFlag:BlockEntity
    {
        bool laststate = false;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            RegisterGameTickListener(OnTick, 10);
        }

        public void OnTick(float df)
        {
            //check for an IElectricty block below, and if it has power mark true
            //  flag block to update if necessary
            bool currentstate = false;
            BlockPos bp = Pos.Copy().Offset(BlockFacing.DOWN);
            BlockEntity checkblock = Api.World.BlockAccessor.GetBlockEntity(bp);
            var bee = checkblock as IElectricity;
            if (bee != null)
            {
                if (bee.IsPowered) { currentstate = true; }
            }
            if (laststate != currentstate) { laststate = currentstate; MarkDirty(true); }//update the block
            
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            if (!laststate) { return base.OnTesselation(mesher, tessThreadTesselator); }
            ICoreClientAPI clientApi = (ICoreClientAPI)Api;
            Block block = Api.World.GetBlock(new AssetLocation("machines:statusflag-up"));
            MeshData mesh = clientApi.TesselatorManager.GetDefaultBlockMesh(block);
            mesher.AddMeshData(mesh);
            
            return true;
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            dsc.AppendLine(laststate.ToString());
            
        }
    }
}
