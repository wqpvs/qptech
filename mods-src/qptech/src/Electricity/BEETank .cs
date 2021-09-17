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
    public class BEETank : BlockEntityContainer
    {
        public int CapacityLitres { get; set; } = 50;

        internal InventoryGeneric inventory;
        public override InventoryBase Inventory => inventory;
        public override string InventoryClassName => "tank";

        MeshData currentMesh;
        BlockTank ownBlock;

        public float MeshAngle;

        public BEETank()
        {
            inventory = new InventoryGeneric(1, null, null);
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            ownBlock = Block as BlockTank;
            if (Api.Side == EnumAppSide.Client)
            {
                currentMesh = GenMesh();
                MarkDirty(true);
            }
           
        }

        protected override void OnTick(float dt)
        {
            if (inventory.Empty) { return; }
            
            //Check for tanks below and beside and fill appropriately
            foreach (BlockFacing bf in BlockFacing.ALLFACES)
            {
                //don't push water up
                if (bf == BlockFacing.UP) { continue; }
                BlockEntityContainer outputContainer = Api.World.BlockAccessor.GetBlockEntity(Pos.Copy().Offset(bf)) as BlockEntityContainer;
                //is it a container?
                if (outputContainer == null) { continue; }
                //is it a fellow tank person?
                BEETank bt = outputContainer as BEETank;
                if (bt == null) { continue; }
                //a null inventory is weird, so skip it
                if (bt.inventory == null) { continue; }
                int outputQuantity = inventory[0].StackSize; 

                //try to fill if container is below us or the container is beside us and has less water than us
                if (bt.inventory[0].StackSize < inventory[0].StackSize||bf==BlockFacing.DOWN)
                {
                    //if it's not below us we want to equalize water instead of filling all of it
                    if (bf != BlockFacing.DOWN)
                    {
                        outputQuantity = (inventory[0].StackSize - outputContainer.Inventory[0].StackSize)/2;
                        if (outputQuantity<=0) { continue; }
                    }
                    WeightedSlot tryoutput = outputContainer.Inventory.GetBestSuitedSlot(inventory[0]);

                    if (tryoutput.slot != null)
                    {
                        ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, outputQuantity);

                        inventory[0].TryPutInto(tryoutput.slot, ref op);
                        
                        MarkDirty(true);
                        bt.MarkDirty(true);
                        if (inventory.Empty || inventory[0].StackSize == 0) { break; }
                        

                    }
                }
            }
        }

        public override void OnBlockBroken()
        {
            // Don't drop inventory contents
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

    }
}