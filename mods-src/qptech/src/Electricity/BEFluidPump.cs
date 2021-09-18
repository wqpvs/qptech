using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Electricity.API;


namespace qptech.src
{
    class BEFluidPump : BlockEntityContainer, IFluidTank
    {
        public int CapacityLitres { get; set; } = 50;

        public bool IsFull { get { return CurrentLevel == CapacityLitres; } }

        public int CurrentLevel => inventory[0].StackSize;

        public Item CurrentItem => inventory[0]==null||inventory[0].Itemstack==null ? null :inventory[0].Itemstack.Item;
        public BEFluidPump()
        {
            inventory = new InventoryGeneric(1, null, null);
        }
        public BlockPos TankPos => Pos;

        public int ReceiveFluidOffer(Item offeredItem, int offeredAmount, BlockPos offerFromPos)
        {
            if (IsFull) { return 0; }
            if (inventory == null) { return 0; }
            if (offeredItem == null) { return 0; }
            if (!inventory.Empty && offeredItem != CurrentItem) { return 0; }
            if (offerFromPos.Y > Pos.Y) { return 0; } //special for pump, we don't want to receive liquid from above us
            int useamount = Math.Min(CapacityLitres - CurrentLevel, offeredAmount);
            if (inventory.Empty)
            {
                ItemStack newstack = new ItemStack(offeredItem, useamount);
                inventory[0].Itemstack = newstack;

            }
            else
            {
                inventory[0].Itemstack.StackSize += useamount;
            }
            MarkDirty(true);
            return useamount;
        }
        internal InventoryGeneric inventory;
        public override InventoryBase Inventory => inventory;
        public override string InventoryClassName => "pump";
        protected override void OnTick(float dt)
        {
            NeighbourCheck();
            Equalize();
        }
        void NeighbourCheck()
        {

        }

        public virtual void Equalize()
        {
            if (!IsFull) { Pump(); }
            if (inventory.Empty) { return; }
            IFluidTank uptank = Api.World.BlockAccessor.GetBlockEntity(Pos.UpCopy()) as IFluidTank;
            if (uptank == null || uptank.IsFull) { return; }
            int used=uptank.ReceiveFluidOffer(CurrentItem, CurrentLevel, Pos);
            if (used == 0) { return; }
            inventory[0].Itemstack.StackSize -= used;
            if (inventory[0].Itemstack.StackSize <= 0) { inventory[0].Itemstack = null; }
            MarkDirty(true);
        }
        void Pump()
        {
            IFluidTank downtank = Api.World.BlockAccessor.GetBlockEntity(Pos.DownCopy()) as IFluidTank;
            if (downtank == null) { return; }
            if (downtank.CurrentLevel == 0) { return; }
            if (downtank.CurrentItem == null) { return; }
            if (!inventory.Empty && downtank.CurrentItem != CurrentItem) { return; }
            Item getitem = downtank.CurrentItem;
            int takeamt = downtank.TryTakeFluid(CapacityLitres - CurrentLevel,Pos);
            if (takeamt == 0) { return; }
            if (inventory.Empty)
            {
                ItemStack newstack = new ItemStack(getitem, takeamt);
                inventory[0].Itemstack = newstack;
                
            }
            else
            {
                inventory[0].Itemstack.StackSize += takeamt;
            }
            
            MarkDirty(true);
        }

        public int TryTakeFluid(int requestedamount, BlockPos offerFromPos)
        {
            int giveamount = 0;
            
            giveamount = Math.Min(requestedamount, CurrentLevel);
            inventory[0].Itemstack.StackSize -= giveamount;
            if (inventory[0].Itemstack.StackSize == 0) { inventory[0].Itemstack = null; }
            MarkDirty(true);
            return giveamount;
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            ItemSlot slot = inventory[0];

            if (slot.Empty)
            {
                dsc.AppendLine(Lang.Get("Empty"));
            }
            else
            {
                dsc.AppendLine(Lang.Get("Contents: {0}x{1}", slot.Itemstack.StackSize, slot.Itemstack.GetName()));
            }
        }
    }
}
