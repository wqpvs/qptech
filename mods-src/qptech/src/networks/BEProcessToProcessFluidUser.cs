using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Client;

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
        int fluidtick = 1000;
        bool usingfluid = false;
        double lastfluiduse = 0;
        double nextfluiduse => lastfluiduse + fluidtick;
        public override bool Running => base.Running && fluidok;
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
            RegisterGameTickListener(OnTick, 75);
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
            usingfluid = false;
            bool ok = base.CheckRequiredProcesses();
            if (!ok) { return false; }
            else { usingfluid = true; }
            if (!fluidok) { return false; }
            MarkDirty();
            return true;
        }

        public override void OnTick(float dt)
        {

            base.OnTick(dt);
            if (Api is ICoreClientAPI) { return; }
            if (Api.World.ElapsedMilliseconds < nextfluiduse && lastfluiduse!=0) { return; }
            fluidok = true;
            if (fluidTankLevel < fluidUse) { fluidok = false; }
            else if (usingfluid) { fluidTankLevel -= fluidUse; }
            lastfluiduse = Api.World.ElapsedMilliseconds;
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
                dsc.Append("Storing " + fluidTankLevel/100 + "/" + fluidTankSize/100 + "L of " + fluid.ToString());
            }
        }
    }
}
