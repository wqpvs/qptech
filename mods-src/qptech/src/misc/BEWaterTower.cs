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
        int tick = 5000;
        int waterPerTick = 1;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            RegisterGameTickListener(OnTick, tick);
        }

        public void OnTick(float tf)
        {
            BlockPos bp = Pos.Copy().Offset(BlockFacing.DOWN);
            BlockEntity checkblock = Api.World.BlockAccessor.GetBlockEntity(bp);
            if (checkblock == null) { return; }
            var checkcontainer = checkblock as BlockEntityContainer;
            if (checkcontainer == null) { return; }
            
            Item outputItem = Api.World.GetItem(new AssetLocation("game:waterportion"));
            ItemStack itmstk = new ItemStack(outputItem, waterPerTick);
            DummyInventory dummy = new DummyInventory(Api);
            dummy[0].Itemstack = itmstk;
            WeightedSlot tryoutput = checkcontainer.Inventory.GetBestSuitedSlot(dummy[0]);
            if (tryoutput.slot == null) { return; }
            
            ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, dummy[0].StackSize);

            dummy[0].TryPutInto(tryoutput.slot, ref op);
            tryoutput.slot.MarkDirty();

            
            //waterportion

        }
    }
}
