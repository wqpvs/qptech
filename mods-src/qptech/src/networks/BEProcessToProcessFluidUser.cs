using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace qptech.src.networks
{
    class BEProcessToProcessFluidUser : BEProcessToProcess, IFluidNetworkUser
    {
        Item fluid;
        int fluidUse;
        int fluidTankSize;
        int fluidTankLevel;
        int requiredFluid => fluidTankSize - fluidTankLevel;
        bool fluidok;
        int fluidtick = 75;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (Block.Attributes != null)
            {
                string fluidcode = Block.Attributes["fluidcode"].AsString("game:waterportion");
                fluid = api.World.GetItem(new AssetLocation(fluidcode));
                fluidUse = Block.Attributes["fluidUse"].AsInt(1);
                fluidTankSize = Block.Attributes["fluidTankSize"].AsInt(fluidUse * 2);
                fluidtick = Block.Attributes["fluidtick"].AsInt(fluidtick);
            }
            RegisterGameTickListener(OnTick, fluidtick);
        }

        public bool IsOnlyDestination()
        {
            return true;
        }

        public bool IsOnlySource()
        {
            return false;
        }

        public int OfferFluid(Item item, int quantity)
        {
            if (item != fluid) { return 0; }
            int used = Math.Min(quantity, requiredFluid);
            if (used < 0) { used = 0; }
            if (used > 0)
            {
                fluidTankLevel += used;
                MarkDirty();
            }
            return used;
        }

        public int QueryFluid(Item item)
        {
            return 0;
        }

        public Item QueryFluid()
        {
            return fluid;
        }

        public int TakeFluid(Item item, int quantity)
        {
            return 0;
        }

        protected override bool CheckRequiredProcesses()
        {
            bool ok = base.CheckRequiredProcesses();
            if (!ok) { return false; }
            if (!fluidok) { return false; }
            MarkDirty();
            return true;
        }

        public void OnTick(float dt)
        {
            fluidok = true;
            if (fluidTankLevel < fluidUse) { fluidok = false; }
            else { fluidTankLevel -= fluidUse; }
            MarkDirty();
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("fluidTankLevel", fluidTankLevel);
            tree.SetBool("fluidok", fluidok);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            fluidok = tree.GetBool("fluidok");
            fluidTankLevel = tree.GetInt("fluidTankLevel");
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if (fluid == null) { dsc.Append("ERROR NO FLUID SET"); }
            else
            {
                dsc.Append("Storing " + fluidTankLevel + "/" + fluidTankSize + "L of " + fluid.ToString());
            }
        }
    }
}
