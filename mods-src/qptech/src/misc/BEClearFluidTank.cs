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

namespace qptech.src
{
    // Concept time
    // - Buckets don't directly hold liquids, they contain itemstacks. In case of liquids they are simply "portions" of that liquid. i.e. a "waterportion" item
    //
    // - The item/block that can be placed into the bucket must have the item/block attribute waterTightContainerProps: { containable: true, itemsPerLitre: 1 }
    //   'itemsPerLitre' defines how many items constitute one litre.

    // - Further item/block more attributes lets you define if a liquid can be obtained from a block source and what should come out when spilled:
    //   - waterTightContainerProps: { containable: true, whenSpilled: { action: "placeblock", stack: { class: "block", code: "water-7" } }  }
    //   or
    //   - waterTightContainerProps: { containable: true, whenSpilled: { action: "dropcontents", stack: { class: "item", code: "honeyportion" } }  }
    // 
    // - BlockBucket has methods for placing/taking liquids from a bucket stack or a placed bucket block

    /// <summary>
    /// TODO Fix up or figure out extended fluid transfer, probably need to not use capacity liters for this!!
    /// </summary>

    public class BEClearFluidTank : BlockEntityContainer, IFluidTank
    {
        
        
        public int CapacityLitres { get; set; } = 100;
        public int CurrentLevel {
            get
            {
                if (inventory == null) { return 0; }
                if (inventory.Empty) { return 0; }
                if (inventory[0] == null) { return 0; }
                
                return inventory[0].StackSize;
                
            }
        }
        public Item CurrentItem
        {
            get
            {
                if (inventory != null && inventory[0] != null && !inventory.Empty)
                {
                    return inventory[0].Itemstack.Item;
                }
                return null;
            }
        }
        public BlockPos TankPos { get { return Pos; } }
        public bool IsFull { get { return CapacityLitres == CurrentLevel; } }
        internal InventoryGeneric inventory;
        public override InventoryBase Inventory => inventory;
        public override string InventoryClassName => "tank";

        MeshData currentMesh;
        BlockTank ownBlock;

        public float MeshAngle;
        BlockFacing[] facechecker = new BlockFacing[] { BlockFacing.DOWN, BlockFacing.NORTH, BlockFacing.EAST, BlockFacing.SOUTH, BlockFacing.WEST };
        public BEClearFluidTank()
        {
            inventory = new InventoryGeneric(1, null, null);
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            //
            //RegisterGameTickListener(OnFluidTick, 1000);
            ownBlock = Block as BlockTank;
            if (Api.Side == EnumAppSide.Client)
            {
                currentMesh = GenMesh();
                MarkDirty(true);
            }
           
        }

        protected override void OnTick(float dt)
        {
            NeighbourCheck();
            Equalize();
        }

        void NeighbourCheck()
        {
            
        }

        public  virtual void Equalize()
        {
            if (inventory.Empty) { return; }
            
            //Check for tanks below and beside and fill appropriately
            foreach (BlockFacing bf in facechecker) //used facechecker to make sure down is processed first
            {

                if (inventory.Empty) { break; }
                BlockEntityContainer outputContainer = Api.World.BlockAccessor.GetBlockEntity(Pos.Copy().Offset(bf)) as BlockEntityContainer;
                if (outputContainer == null) { continue; }              //is it a container?
                IFluidTank bt = outputContainer as IFluidTank;
                if (bt == null) { continue; }                           //is it a fellow tank person?
                if (outputContainer.Inventory == null) { continue; }    //a null inventory is weird, so skip it
                int outputQuantity = inventory[0].StackSize;            //default to dumping entire stack
                if (bf != BlockFacing.DOWN && bt.CurrentLevel > CurrentLevel) { continue; }    //beside us and already has more liquid
                if (bt.IsFull) { continue; }                         //its already full
                if (bf != BlockFacing.DOWN) {
                    int targetQuantity = (CurrentLevel + bt.CurrentLevel) / 2;
                    outputQuantity = CurrentLevel - targetQuantity;
                }
                int usedQuantity = bt.ReceiveFluidOffer(inventory[0].Itemstack.Item, outputQuantity,Pos);
                if (usedQuantity > 0)
                {
                    inventory[0].Itemstack.StackSize -= usedQuantity;
                    if (inventory[0].Itemstack.StackSize <= 0)
                    {
                        inventory[0].Itemstack = null;
                    }
                    MarkDirty(true);
                }
            }
        }

        public int ReceiveFluidOffer(Item offeredItem, int offeredAmount,BlockPos offeredFromPos)
        {
            if (inventory[0].Itemstack!=null&&inventory[0].Itemstack.Item!=null&&offeredItem != inventory[0].Itemstack.Item) { return 0; }
            int useamount = offeredAmount;
            useamount = Math.Min(CapacityLitres - CurrentLevel, useamount);
            if (useamount <= 0) { useamount=0; }
            else if (inventory[0].Itemstack == null || inventory[0].Itemstack.Item == null)
            {
                ItemStack newstack = new ItemStack(offeredItem, useamount);
                inventory[0].Itemstack = newstack;
                MarkDirty(true);
            }
            else
            {
                inventory[0].Itemstack.StackSize += useamount;
                MarkDirty(true);
            }
            
            offeredAmount -= useamount;
            ///TODO Here we could push overflow?
            return useamount;
        }
  
        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);

            if (Api.Side == EnumAppSide.Client)
            {
                currentMesh = GenMesh();
                MarkDirty(true);
            }
        }

        public ItemStack GetContent()
        {
            return inventory[0].Itemstack;
        }


        internal void SetContent(ItemStack stack)
        {
            inventory[0].Itemstack = stack;
            MarkDirty(true);
        }



        internal MeshData GenMesh()
        {
            if (ownBlock == null) return null;

            MeshData mesh = ownBlock.GenMesh(Api as ICoreClientAPI, GetContent(), Pos);

            if (mesh.CustomInts != null)
            {
                for (int i = 0; i < mesh.CustomInts.Count; i++)
                {
                    mesh.CustomInts.Values[i] |= 1 << 27; // Disable water wavy
                    mesh.CustomInts.Values[i] |= 1 << 26; // Enabled weak foam
                }
            }

            return mesh;
        }

        public void OnBlockInteract(IPlayer byPlayer)
        {
       
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            mesher.AddMeshData(currentMesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, 0, 0));
            return true;
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);

            if (Api?.Side == EnumAppSide.Client)
            {
                currentMesh = GenMesh();
                MarkDirty(true);
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

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
        public int TryTakeFluid(int requestedamount, BlockPos offerFromPos)
        {
            int giveamount = 0;

            giveamount = Math.Min(requestedamount, CurrentLevel);
            inventory[0].Itemstack.StackSize -= giveamount;
            if (inventory[0].Itemstack.StackSize == 0) { inventory[0].Itemstack = null; }
            MarkDirty(true);
            return giveamount;
        }

    }
}