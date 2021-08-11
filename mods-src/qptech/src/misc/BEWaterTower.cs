using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Server;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;
using qptech.src.extensions;

namespace qptech.src
{
    class BEWaterTower:BlockEntity
    {
        int tick = 7*5000;
        int waterPerTick = 1;
        int bonusRainWaterPerTick = 1; //this is in liters, not a multiplier
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            tick = Block.Attributes["tick"].AsInt(tick);
            waterPerTick = Block.Attributes["waterPerTick"].AsInt(waterPerTick);
            bonusRainWaterPerTick = Block.Attributes["bonusRainWaterPerTick"].AsInt(bonusRainWaterPerTick);
            RegisterGameTickListener(OnTick, tick);
        }

        public void OnTick(float tf)
        {
            //Make sure there is a valid container
            BlockPos bp = Pos.Copy().Offset(BlockFacing.DOWN);
            BlockEntity checkblock = Api.World.BlockAccessor.GetBlockEntity(bp);
            if (checkblock == null) { return; }
            var checkcontainer = checkblock as BlockEntityContainer;
            if (checkcontainer == null) { return; }
            //Make sure block is exposed to sky
            int worldheight = Api.World.BlockAccessor.GetRainMapHeightAt(Pos);
            if (worldheight > Pos.Y) { return; }

            //Setup some water to insert
            int waterqty = waterPerTick;
            int rainnear = Api.World.BlockAccessor.GetDistanceToRainFall(Pos);
            if (rainnear < 99) { 
                waterqty+=bonusRainWaterPerTick;
            }
            Item outputItem = Api.World.GetItem(new AssetLocation("game:waterportion"));
            ItemStack itmstk = new ItemStack(outputItem, waterqty);
            DummyInventory dummy = new DummyInventory(Api);
            dummy[0].Itemstack = itmstk;

            WeightedSlot tryoutput = checkcontainer.Inventory.GetBestSuitedSlot(dummy[0]);
            if (tryoutput.slot != null) {
                ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, dummy[0].StackSize);

                dummy[0].TryPutInto(tryoutput.slot, ref op);
                tryoutput.slot.MarkDirty();
                return;
            }
            
            /*for (int c = 0; c < checkcontainer.Inventory.Count;c++)
            {
                ItemSlotLiquidOnly lo = checkcontainer.Inventory[c] as ItemSlotLiquidOnly;
                if (lo == null) { continue; }
                if (lo.CanHold(dummy[0]))
                {
                    ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, dummy[0].StackSize);

                    dummy[0].TryPutInto(tryoutput.slot, ref op);
                    tryoutput.slot.MarkDirty();
                    return;
                }

            }*/
            
            
            

            
            //waterportion

        }
    }
}
