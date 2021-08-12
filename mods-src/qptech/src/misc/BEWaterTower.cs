﻿using System;
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
        int internalStorageCapacity = 32;
        int waterStored = 0;
        int bonusRainWaterPerTick = 1; //this is in liters, not a multiplier
        bool structurecomplete = false;
        bool opentosky = false;
        MultiblockStructure ms;
        public BlockPos mboffset;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            tick = Block.Attributes["tick"].AsInt(tick);
            waterPerTick = Block.Attributes["waterPerTick"].AsInt(waterPerTick);
            bonusRainWaterPerTick = Block.Attributes["bonusRainWaterPerTick"].AsInt(bonusRainWaterPerTick);
            internalStorageCapacity = Block.Attributes["internalStorageCapacity"].AsInt(internalStorageCapacity);
            ms = Block.Attributes["multiblockStructure"].AsObject<MultiblockStructure>();
            int[] offsarray = { 0, 0, 0 };
            offsarray = Block.Attributes["mboffset"].AsArray<int>(offsarray);
            mboffset = new BlockPos(offsarray[0]+Pos.X,offsarray[1]+Pos.Y,offsarray[2]+Pos.Z);
            
            ms.InitForUse(0);
            RegisterGameTickListener(OnTick, tick);
        }
        bool CheckClearSky()
        {
            int worldheight = Api.World.BlockAccessor.GetRainMapHeightAt(Pos);
            if (worldheight > Pos.Y) { return false; }
            return true;
        }
        bool CheckCompleteStructure()
        {
            if (ms==null) { return false; }
            if (ms.InCompleteBlockCount(Api.World, mboffset) > 0)
            {
                
                return false;
            }
            return true;
        }
        public void OnTick(float tf)
        {
            //Make sure there is a valid container
            structurecomplete = CheckCompleteStructure();
            if (!structurecomplete) { return; }
            opentosky = CheckClearSky();
            if (!opentosky) { return; }
            BlockPos bp = Pos.Copy().Offset(BlockFacing.DOWN);
            BlockEntity checkblock = Api.World.BlockAccessor.GetBlockEntity(bp);
            int waterqty = waterPerTick;
            int rainnear = Api.World.BlockAccessor.GetDistanceToRainFall(Pos);
            if (rainnear < 99)
            {
                waterqty += bonusRainWaterPerTick;
            }
            waterStored += waterqty;
            if (waterStored > internalStorageCapacity) { waterStored = internalStorageCapacity; }
            if (checkblock == null) { return; }
            var checkcontainer = checkblock as BlockEntityContainer;
            if (checkcontainer == null) { return; }


            //Setup some water to insert
           
            Item outputItem = Api.World.GetItem(new AssetLocation("game:waterportion"));
            ItemStack itmstk = new ItemStack(outputItem, waterStored);
            DummyInventory dummy = new DummyInventory(Api);
            dummy[0].Itemstack = itmstk;

            WeightedSlot tryoutput = checkcontainer.Inventory.GetBestSuitedSlot(dummy[0]);
            if (tryoutput.slot != null) {
                ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, dummy[0].StackSize);

                int usedwater=dummy[0].TryPutInto(tryoutput.slot, ref op);
                waterStored -= usedwater;
                if (waterStored < 0) { waterStored = 0; }
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
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if (!structurecomplete)
            {
                dsc.AppendLine("STRUCTURE NOT COMPLETE!");
                return;
            }
            if (!opentosky)
            {
                dsc.AppendLine("WATER TOWER SKY ACCESS IS BLOCKED!");
            }

            dsc.AppendLine("Stored Water " + waterStored/16 +"/"+internalStorageCapacity/16+"L");
            
            //dsc.AppendLine("IN:" + inputConnections.Count.ToString() + " OUT:" + outputConnections.Count.ToString());
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);


            //if (type == null) type = defaultType; // No idea why. Somewhere something has no type. Probably some worldgen ruins
            waterStored = tree.GetInt("waterStored");

        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            tree.SetInt("waterStored", waterStored);

        }
        bool showingcontents = false;
        public bool Interact(IPlayer byPlayer)
        {
            showingcontents = !showingcontents;
            opentosky = CheckClearSky();
            structurecomplete = CheckCompleteStructure();
            if (Api.Side == EnumAppSide.Client)
            {
                if (showingcontents)
                {
                    ms.HighlightIncompleteParts(Api.World, byPlayer, mboffset);
                }
                else
                {
                    ms.ClearHighlights(Api.World, byPlayer);
                }
            }
            return true;
        }
    }
}
