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
        

        //TODO set this up with a proper liquid itemstack

        
        ItemStack itemstack;
        int CapacityLiters => 1000;
        public BlockPos TankPos => Pos;

        public override void Initialize(ICoreAPI api)
        {
            
            base.Initialize(api);
        }

        

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if (itemstack == null || itemstack.Item == null||itemstack.StackSize==0) { dsc.AppendLine("Internal Tank is Empty"); }
            else
            {
                dsc.AppendLine("Internal Tank Contains " + itemstack.StackSize/100 + " L of " + itemstack.Item.GetHeldItemName(itemstack));
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            itemstack = tree.GetItemstack("itemstack");
            if (itemstack != null)
            {
                itemstack.ResolveBlockOrItem(worldAccessForResolve);
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetItemstack("itemstack", itemstack);
        }

        public int TakeFluid(Item item, int amt) { return 0; }

        public int QueryFluid(Item item)
        {
            if (itemstack == null || itemstack.Item == null)
            {
                
                return item.MaxStackSize;
            }
            else if (itemstack.Item == item)
            {
                if (itemstack.StackSize < item.MaxStackSize)
                {
                    int used = (item.MaxStackSize - itemstack.StackSize);
                    return used;
                }
            }
            return 0;
        }

        public int OfferFluid(Item item, int quantity)
        {
            
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
            return null;
        }

        public bool IsOnlySource()
        {
            return false;
        }

        public bool IsOnlyDestination()
        {
            return true;
        }
    }
}
