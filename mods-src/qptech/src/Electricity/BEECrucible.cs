using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using qptech.src.networks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace qptech.src
{
    /// <summary>
    /// Simple electric crucible - gets nuggets, heats, and exports ingots
    /// BEElectric Crucible will be the larger version with complex interface etc
    /// </summary>
    class BEECrucible:BEEBaseDevice
    {
        //check input chest for nuggets
        //if found (and enough there) look up the combustiblePropsByType.meltingPoint
        //check if enough heat supplied (firepit, or Industiral Process?)
        //if there is enough then take the nuggets begin processing
        //output ingot when done, heated appropriately
        int capacityIngots = 1;
        protected BlockFacing rmInputFace;
        protected BlockFacing outputFace;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (Block.Attributes != null)
            {
                capacityIngots = Block.Attributes["capacityIngots"].AsInt(capacityIngots);
                rmInputFace = BlockFacing.FromCode(Block.Attributes["inputFace"].AsString("up"));
                outputFace = BlockFacing.FromCode(Block.Attributes["outputFace"].AsString("down"));
                rmInputFace = OrientFace(Block.Code.ToString(), rmInputFace);
                outputFace = OrientFace(Block.Code.ToString(), outputFace);
            }
        }

        protected override void DoDeviceStart()
        {
            IBlockEntityContainer container = Api.World.BlockAccessor.GetBlockEntity(Pos.Copy().Offset(rmInputFace)) as IBlockEntityContainer;
            if (container == null) { return; }
            if (container.Inventory == null) { return; }
            if (container.Inventory.Empty) { return; }
            List<ItemStack> stacks = new List<ItemStack>();
            int totalqty = 0;
            foreach (ItemSlot slot in container.Inventory)
            {
                //checks for valid items
                if (slot == null || slot.Empty) { continue; }
                if (slot.Itemstack == null || slot.Itemstack.StackSize <= 0) { continue; }
                if (slot.Itemstack.Item == null) { continue; }
                if (slot.Itemstack.Item.CombustibleProps == null) { continue; }
                if (slot.Itemstack.Item.CombustibleProps.SmeltedStack == null) { continue; }
                AssetLocation mat = slot.Itemstack.Item.CombustibleProps.SmeltedStack.Code;
                int qty = slot.Itemstack.StackSize;
                float temp = slot.Itemstack.Collectible.GetTemperature(Api.World, slot.Itemstack);
                //Exclude items that aren't heated enough
                if (temp < slot.Itemstack.Item.CombustibleProps.MeltingPoint) { continue; }
                //Add up how many items
                if (slot.Itemstack.Item.CombustibleProps.SmeltedStack.StackSize >= 1)
                {
                    qty = slot.Itemstack.Item.CombustibleProps.SmeltedStack.StackSize * 20*slot.Itemstack.StackSize;
                }
                totalqty += qty;
                stacks.Add(slot.Itemstack);
            }
            //If there is nothing valid, then don't do anything
            if (stacks.Count() <= 0) { return; }
            //Figure out what alloys can be made (if any)
            AlloyRecipe canmake = GetMatchingAlloy(Api.World, stacks.ToArray());
            if (canmake != null)
            {
                IBlockEntityContainer outcontainer = Api.World.BlockAccessor.GetBlockEntity(Pos.Copy().Offset(outputFace)) as IBlockEntityContainer;
                DummyInventory di = new DummyInventory(Api,1);
                di[0].Itemstack = new ItemStack(canmake.Output.ResolvedItemstack.Item,totalqty/20);
               
               
                if (outcontainer != null)
                {
                    foreach (ItemSlot tryslot in outcontainer.Inventory)
                    {
                        if (tryslot != null)
                        {
                            ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, di[0].StackSize);

                            int used = di[0].TryPutInto(tryslot, ref op);
                            
                            if (di[0].Itemstack==null|| di[0].Itemstack.StackSize <= 0) { break; }
                            di[0].Itemstack.StackSize = used;
                        }
                    }
                    (outcontainer as BlockEntity).MarkDirty();
                }
                if (!di.Empty) { di.DropAll(Pos.ToVec3d()); }
                
                foreach (ItemSlot slot in container.Inventory)
                {
                    slot.Itemstack = null;
                }
                (container as BlockEntity).MarkDirty();

            }
        }
        public AlloyRecipe GetMatchingAlloy(IWorldAccessor world, ItemStack[] stacks)
        {
            List<AlloyRecipe> alloys = Api.GetMetalAlloys();
            if (alloys == null) return null;

            for (int j = 0; j < alloys.Count; j++)
            {
                if (alloys[j].Matches(stacks))
                {
                    return alloys[j];
                }
            }

            return null;
        }
    }
}
