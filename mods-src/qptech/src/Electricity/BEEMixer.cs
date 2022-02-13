using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.API.Client;
using qptech.src.networks;

namespace qptech.src.Electricity
{
    class BEEMixer : BEERecipeProcessor, IFluidNetworkUser { 
        //InventoryGeneric inventory;
        //Need - dry input, dry output, liquid input, liquid output
        //   - should track time sealed if necessary, have a processing speed bonus
        //   - would only use as much material as can be used, will store remaining
        //   - needs a purge function
        // Should I use an input tank instead of internal IFluidTank?
        

        //TODO add purge mode

        
        ItemStack itemstack;
        
        public BlockPos TankPos => Pos;
        bool purgemode = false;
        public override void Initialize(ICoreAPI api)
        {
            
            base.Initialize(api);
        }

        protected override void AddAdditionalIngredients(ref Dictionary<string, int> ingredientlist)
        {
            if (itemstack != null && itemstack.Item != null && itemstack.StackSize > 0)
            {
                string key = itemstack.Item.Code.ToString();
                if (ingredientlist.ContainsKey(key)) { ingredientlist[key] += itemstack.StackSize; }
                else { ingredientlist[key] = itemstack.StackSize; }
            }
            int test = 1;
        }

        protected override int TryAdditionalDraw(MachineRecipeItems mi, int needqty)
        {
            if (itemstack != null && itemstack.Item != null && itemstack.StackSize > 0)
            {
                string key = itemstack.Item.Code.ToString();
                if (mi.Match(key))
                {
                    int drawamt = Math.Min(needqty, itemstack.StackSize);
                    itemstack.StackSize -= drawamt;
                    MarkDirty();
                    return drawamt;
                }
            }
            return base.TryAdditionalDraw(mi, needqty);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if (itemstack == null || itemstack.Item == null||itemstack.StackSize==0) { dsc.AppendLine("Internal Tank is Empty"); }
            else
            {
                dsc.AppendLine("Internal Tank Contains " + itemstack.StackSize/100 + " L of " + itemstack.Item.GetHeldItemName(itemstack));
            }
            if (purgemode) { dsc.AppendLine("PURGING"); }
            else { dsc.AppendLine("FILLING"); }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            purgemode = tree.GetBool("purgemode", purgemode);
            itemstack = tree.GetItemstack("itemstack");
            if (itemstack != null)
            {
                itemstack.ResolveBlockOrItem(worldAccessForResolve);
            }
        }
        public override void Wrench()
        {
            base.Wrench();
            purgemode = !purgemode;
            if (Api is ICoreServerAPI&&purgemode)
            {
                TryPurge();
            }
            MarkDirty();
        }

        public virtual void TryPurge()
        {
            if (itemstack == null || itemstack.StackSize == 0 || itemstack.Item == null) { return; }
            BlockPos purgepos = Pos.Copy().Offset(BlockFacing.DOWN);
            IFluidTank purgetank = Api.World.BlockAccessor.GetBlockEntity(purgepos) as IFluidTank;
            if (purgetank != null) { purgetank.ReceiveFluidOffer(itemstack.Item, itemstack.StackSize, Pos); }
            itemstack = null;          
            
            MarkDirty();
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetItemstack("itemstack", itemstack);
            tree.SetBool("purgemode", purgemode);
        }
        bool fluidmoveablestate => deviceState == enDeviceState.MATERIALHOLD || deviceState == enDeviceState.IDLE;
        bool shouldgivefluid => purgemode && IsOn && fluidmoveablestate;
        bool shouldtakefluid => !purgemode && fluidmoveablestate;
        public int TakeFluid(Item item, int amt) {
            if (!shouldgivefluid) { return 0; }
            if (itemstack == null || itemstack.Item == null) { return 0; }
            if (itemstack.Item != item) { return 0; }
            int stackdraw = Math.Min(amt, itemstack.StackSize);
            itemstack.StackSize -= stackdraw;
            if (itemstack.StackSize <= 0) { itemstack = null;  }
            MarkDirty();
            return stackdraw;
        }

        public int QueryFluid(Item item)
        {
            if (!shouldgivefluid) { return 0; }
            if (itemstack == null || itemstack.Item == null)
            {
                
                return 0;
            }
            else if (itemstack.Item == item)
            {
                return itemstack.StackSize;
            }
            return 0;
        }

        public int OfferFluid(Item item, int quantity)
        {
            if (!shouldtakefluid) { return 0; }
            if (itemstack == null || itemstack.Item==null||itemstack.StackSize==0)
            {
                itemstack = new ItemStack(item, quantity);
                MarkDirty();
                return quantity;
            }
            else if (itemstack.Item == item)
            {
                if (itemstack.StackSize < item.MaxStackSize)
                {
                    int used = Math.Min(item.MaxStackSize - itemstack.StackSize,quantity);
                    itemstack.StackSize += used;
                    return used;
                }
            }
            return 0;
        }

        public Item QueryFluid()
        {
            if (!shouldtakefluid) { return null; }
            if (itemstack == null) { return null; }
            return itemstack.Item;
        }

        public bool IsOnlySource()
        {
            //return false;
            return false;
        }

        public bool IsOnlyDestination()
        {
            //return false;
            return true;
        }
    }
}
