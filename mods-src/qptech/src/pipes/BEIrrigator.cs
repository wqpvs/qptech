using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using qptech.src.networks;
namespace qptech.src.pipes
{
    class BEIrrigator : BlockEntity, IFluidNetworkUser
    {
        
        int waterUsage= 1;
        int internalwater = 0;
        int internaltank = 0;
        int range = 1;
        Item usefluid;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            internaltank = waterUsage*2;
            fluiditem = api.World.GetItem(new AssetLocation("game:waterportion"));
            RegisterGameTickListener(OnTick, 500);
        }

        public void OnTick(float df)
        {
            if (!(Api is ICoreClientAPI) && internaltank > waterUsage)
            {
                DoIrrigation();
            }
        }

        Item fluiditem;
        public Item QueryFluid()
        {
            
            return fluiditem;
        }
        public int OfferFluid(Item item, int amt)
        {
            
            if (internalwater >= internaltank) { return 0; }
            int used = Math.Min(internaltank - internalwater, amt);
            if (used > 0)
            {
                internalwater += used;
                MarkDirty();
            }
            return used;

        }

        public int TakeFluid(Item item, int quantity)
        {
            return 0;
        }

        public int QueryFluid(Item item)
        {
            return 0;
        }

        void DoIrrigation()
        {
            if (internalwater < waterUsage) { return; }
            
            bool usedany = false;
            for (int xc = -range; xc <= range; xc++)
            {
                for (int zc = -range; zc <= range; zc++)
                {
                    BlockPos checkp = Pos.UpCopy();
                    checkp.Z += zc;
                    checkp.X += xc;
                    BlockEntityFarmland fl = Api.World.BlockAccessor.GetBlockEntity(checkp) as BlockEntityFarmland;
                    if (fl == null) { continue; }
                    float moist = fl.MoistureLevel;
                    if (moist < 0.9f)
                    {
                        fl.WaterFarmland(1f, true);
                        usedany = true;
                    }
                }
            }
            if (usedany)
            {
                internalwater -= waterUsage;
                MarkDirty();
            }
            
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            internalwater = tree.GetInt("internalwater", 0);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("internalwater",internalwater);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            dsc.Append("internal tank: " + internalwater + " L");
        }
    }
}
