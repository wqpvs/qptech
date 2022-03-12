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
        public float capacityliters = 50;
        public float CapacityLiters => capacityliters;
        
        public BlockPos TankPos => Pos;
        bool purgemode = false;
        public override void Initialize(ICoreAPI api)
        {
            
            base.Initialize(api);
            if (Block.Attributes != null)
            {
                capacityliters = Block.Attributes["CapacityLiters"].AsFloat(capacityliters);
            }
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
        protected override void DoDeviceComplete()
        {
            //create items in attached inventories or in world
            DummyInventory di = new DummyInventory(Api, 1);
            MachineRecipe mr = recipes.Find(x => x.name == makingrecipe);
            BlockEntityContainer outcont = Api.World.BlockAccessor.GetBlockEntity(Pos.Copy().Offset(matOutputFace)) as BlockEntityContainer;
            
            if (mr == null) { deviceState = enDeviceState.ERROR; return; }
            foreach (MachineRecipeItems outitem in mr.output)
            {

                AssetLocation al = new AssetLocation(outitem.validitems[0]);
                Item makeitem = Api.World.GetItem(al);
                Block makeblock = Api.World.GetBlock(al);
                if (makeitem == null && makeblock == null) { deviceState = enDeviceState.ERROR; return; }
                ItemStack newstack = null;
                if (makeitem != null) { newstack = new ItemStack(makeitem, outitem.quantity); }
                else { newstack = new ItemStack(makeblock, outitem.quantity); }
                if (newstack.Item!=null&&newstack.Item.Attributes.KeyExists("waterTightContainerProps"))
                {
                    WaterTightContainableProps liquidpros = newstack.Item.Attributes["waterTightContainerProps"].AsObject<WaterTightContainableProps>();
                    newstack.StackSize *= (int)liquidpros.ItemsPerLitre;
                    overridestate = true;
                    int used = this.OfferFluid(newstack.Item, newstack.StackSize);
                    overridestate = false;
                    if (used == 0) { return; }
                    MarkDirty();
                    deviceState = enDeviceState.IDLE;
                    ResetTimers();
                    makingrecipe = "";
                    return;
                }
                di[0].Itemstack = newstack;
                if (mr.processingsteps != null && mr.processingsteps.ContainsKey("heating"))
                {
                    di[0].Itemstack.Collectible.SetTemperature(Api.World, di[0].Itemstack, (float)mr.processingsteps["heating"]);
                }
                if (outcont != null)
                {

                    foreach (ItemSlot tryslot in outcont.Inventory)
                    {

                        if (di.Empty) { break; }


                        int rem = di[0].TryPutInto(Api.World, tryslot);
                        MarkDirty();
                        di.MarkSlotDirty(0);
                    }
                }
                //di[0].Itemstack.Collectible.SetTemperature(Api.World, di[0].Itemstack, mr.temperature);

                di.DropAll(Pos.Copy().Offset(matOutputFace).ToVec3d());
            }
            deviceState = enDeviceState.IDLE;
            ResetTimers();
            makingrecipe = "";
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetItemstack("itemstack", itemstack);
            tree.SetBool("purgemode", purgemode);
        }
        
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
            if (item == null) { return 0; }
            WaterTightContainableProps waterprops = item.Attributes["waterTightContainerProps"].AsObject<WaterTightContainableProps>();
            if (waterprops == null) { return 0; }

            if (itemstack == null || itemstack.Item==null||itemstack.StackSize==0)
            {
                itemstack = new ItemStack(item, quantity);
                MarkDirty();
                return quantity;
            }
            else if (itemstack.Item == item)
            {
                float maxfit = (CapacityLiters *waterprops.ItemsPerLitre)-itemstack.StackSize;
                if (maxfit>0)
                {
                    
                    int used = Math.Min((int)maxfit, quantity);
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
