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
            if (Api is ICoreServerAPI)
            {
                purgemode = !purgemode;
                MarkDirty();
            }
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetItemstack("itemstack", itemstack);
            tree.SetBool("purgemode", purgemode);
        }

        public int TakeFluid(Item item, int amt) {
            if (!purgemode) { return 0; }
            if (itemstack == null || itemstack.Item == null) { return 0; }
            if (itemstack.Item != item) { return 0; }
            int stackdraw = Math.Min(amt, itemstack.StackSize);
            itemstack.StackSize -= stackdraw;
            if (itemstack.StackSize <= 0) { itemstack = null; purgemode = false; }
            MarkDirty();
            return stackdraw;
        }

        public int QueryFluid(Item item)
        {
            if (!purgemode) { return 0; }
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
            if (purgemode) { return 0; }
            if (itemstack == null || itemstack.Item==null)
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
            if (!purgemode) { return null; }
            if (itemstack == null) { return null; }
            return itemstack.Item;
        }

        public bool IsOnlySource()
        {
            return purgemode;
        }

        public bool IsOnlyDestination()
        {
            return !purgemode;
        }
    }
}
