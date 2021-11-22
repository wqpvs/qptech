﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using qptech.src.networks;

namespace qptech.src
{
    class BEERecipeProcessor:BEEBaseDevice
    {
        protected static List<MachineRecipe> masterrecipelist;
        public static List<MachineRecipe> MasterRecipeList => masterrecipelist;
        double recipefinishedat;
        string makingrecipe="";
        BlockFacing matInputFace = BlockFacing.WEST;
        BlockFacing matOutputFace = BlockFacing.EAST;
        List<MachineRecipe> recipes;
        string statusmessage;
        
        public static void LoadRecipes(ICoreAPI api)
        {
            if (masterrecipelist == null)
            {
                masterrecipelist = api.Assets.TryGet("qptech:config/machinerecipes.json").ToObject<List<MachineRecipe>>();
            }
        }
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            LoadRecipes(api);
            if (Block.Attributes != null)
            {
                string[] recipegroups = Block.Attributes["recipegroups"].AsArray<string>();
                recipes = new List<MachineRecipe>();
                if (recipegroups != null)
                {
                    foreach (MachineRecipe mr in MasterRecipeList)
                    {
                        if (recipegroups.Contains(mr.name))
                        {
                            recipes.Add(mr);
                        }
                    }

                }
                matInputFace = OrientFace(Block.Code.ToString(), matInputFace);
                matOutputFace = OrientFace(Block.Code.ToString(), matOutputFace);
            }

        
        }
        protected override void UsePower()
        {
            statusmessage = "";
            if (!IsPowered || !isOn) {
                if (makingrecipe != "") {
                    recipefinishedat = Api.World.Calendar.TotalHours + 0.1;MarkDirty();
                }
                
                return;
            }

            enDeviceState ogdevicestate = deviceState;

            if (makingrecipe == "")
            {
                DoDeviceStart();
            }
            else if (Api.World.Calendar.TotalHours >= recipefinishedat)
            {
                DoDeviceComplete();
            }
            else if (Api.World.Calendar.TotalHours < recipefinishedat)
            {
                if (!CheckProcessing()){
                    recipefinishedat = Api.World.Calendar.TotalHours + 0.1; MarkDirty();
                    deviceState = enDeviceState.PROCESSHOLD;
                }
                else
                {
                    deviceState = enDeviceState.RUNNING;
                }
            }
            else { deviceState = enDeviceState.IDLE; }
            MarkDirty();
        }
        bool CheckProcessing()
        {
            MachineRecipe mr = recipes.Find(m => m.name == makingrecipe);
            if (mr == null) { return false; }
            if (mr.processingsteps.Count == 0) { return true; }
            
            List<string> todo = mr.processingsteps.Keys.ToList<string>();
            BlockFacing[] processfacings = BlockFacing.ALLFACES;
            foreach (BlockFacing bf in processfacings)
            {
                if (todo.Count == 0) { return true; }
                IProcessingSupplier ips = Api.World.BlockAccessor.GetBlockEntity(Pos.Copy().Offset(bf)) as IProcessingSupplier;
                if (ips == null) { continue; }
                foreach (string key in mr.processingsteps.Keys)
                {
                    if (ips.RequestProcessing(key, mr.processingsteps[key])&&todo.Contains(key)) { todo.Remove(key); }
                }
            }
            if (todo.Count == 0) { return true; }
            statusmessage = "MISSING PROCESSORS [";
            foreach (string missing in todo)
            {
                statusmessage += missing + "(" + mr.processingsteps[missing] + "), ";
            }
            statusmessage += "]";
            return false;
        }
        protected override void DoDeviceStart()
        {
            //Check for valid recipes
            if (recipes is null || recipes.Count == 0) { deviceState = enDeviceState.ERROR;return; }
            //Check for ingredients
            Dictionary<string, int> availableingredients= new Dictionary<string, int>();
            
            BlockFacing checkface = matInputFace;
            BlockPos checkpos = Pos.Copy().Offset(checkface);

            BlockEntity checkbe = Api.World.BlockAccessor.GetBlockEntity(checkpos);
            if (checkbe == null) { return; }
            BlockEntityContainer checkcont = checkbe as BlockEntityContainer;
            if (checkcont == null) { return; }
            InventoryBase checkinv = checkcont.Inventory;
            if (checkinv == null || checkinv.Empty) { return; }

            //add up all available ingredients
            foreach (ItemSlot checkslot in checkinv)
            {
                if (checkslot == null || checkslot.Empty) { continue; }
                ItemStack checkstack = checkslot.Itemstack;
                if (checkstack == null | checkstack.StackSize == 0 || (checkstack.Item == null&&checkstack.Block==null)) { continue; }
                string checkcode = "";
                
                if (checkstack.Item != null) { checkcode = checkstack.Item.Code.ToString(); }
                else { checkcode = checkstack.Block.Code.ToString();  }
                if (!availableingredients.ContainsKey(checkcode)) { availableingredients[checkcode] = checkstack.StackSize; }
                else { availableingredients[checkcode] += checkstack.StackSize; }
            }

            MachineRecipe canmake = null;
            foreach (MachineRecipe mr in recipes)
            {
                bool ok = true;
                foreach (MachineRecipeItems mi in mr.ingredients)
                {
                    int foundcount = 0;
                    foreach (string checking in mi.validitems)
                    {
                        if (availableingredients.ContainsKey(checking))
                        {
                            foundcount += availableingredients[checking];
                            if (foundcount >= mi.quantity) { break; }
                        }
                    }
                    if (foundcount < mi.quantity) { ok = false; break; }
                }
                if (!ok) { continue; }
                else { canmake = mr;break; }
            }
            if (canmake == null) { return; }
            //Draw inventory
            foreach (MachineRecipeItems mi in canmake.ingredients)
            {
                int countdown = mi.quantity;
                foreach (ItemSlot checkslot in checkinv)
                {
                    if (checkslot.Empty) { continue; }
                    
                    string checkcode = "";
                    ItemStack checkstack = checkslot.Itemstack;

                    bool isblock = false;
                    if (checkstack.Item != null) { checkcode = checkstack.Item.Code.ToString(); }
                    else { checkcode = checkstack.Block.Code.ToString(); isblock = true; }
                    if (!mi.validitems.Contains(checkcode)) { continue; }
                    int maxtake = Math.Min(countdown, checkstack.StackSize);
                    checkstack.StackSize -= maxtake;
                    countdown -= maxtake;
                    if (checkstack.StackSize <= 0) { checkslot.Itemstack = null; }
                    checkslot.MarkDirty();
                    if (countdown <= 0) { break; }
                }
            }


            //Start Processing if all is good, set state
            makingrecipe = canmake.name;
            recipefinishedat = Api.World.Calendar.TotalHours + canmake.processinghours;
            deviceState = enDeviceState.RUNNING;
        }

        protected override void DoDeviceComplete()
        {
            //create items in attached inventories or in world
            DummyInventory di = new DummyInventory(Api,1);
            MachineRecipe mr = recipes.Find(x => x.name == makingrecipe);
            BlockEntityContainer outcont = Api.World.BlockAccessor.GetBlockEntity(Pos.Copy().Offset(matOutputFace)) as BlockEntityContainer;

            if (mr == null) { deviceState = enDeviceState.ERROR;return; }
            foreach (MachineRecipeItems outitem in mr.output)
            {
                
                AssetLocation al = new AssetLocation(outitem.validitems[0]);
                Item makeitem = Api.World.GetItem(al);
                Block makeblock = Api.World.GetBlock(al);
                if (makeitem == null && makeblock == null) { deviceState = enDeviceState.ERROR;return; }
                ItemStack newstack=null;
                if (makeitem != null) { newstack = new ItemStack(makeitem, outitem.quantity); }
                else { newstack = new ItemStack(makeblock, outitem.quantity); }
                
                di[0].Itemstack = newstack;
                if (mr.processingsteps.ContainsKey("heating"))
                {
                    di[0].Itemstack.Collectible.SetTemperature(Api.World, di[0].Itemstack, (float)mr.processingsteps["heating"]);
                }
                if (outcont != null)
                {
                    
                    foreach (ItemSlot tryslot in outcont.Inventory)
                    {
                        
                        if (di.Empty) { break; }


                        int rem=di[0].TryPutInto(Api.World, tryslot);
                        MarkDirty();
                        di.MarkSlotDirty(0);
                    }
                }              
                //di[0].Itemstack.Collectible.SetTemperature(Api.World, di[0].Itemstack, mr.temperature);
                
                di.DropAll(Pos.Copy().Offset(matOutputFace).ToVec3d());
            }
            deviceState = enDeviceState.IDLE;
            makingrecipe = "";
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetString("makingrecipe", makingrecipe);
            tree.SetDouble("recipefinishedat", recipefinishedat);
            tree.SetString("statusmessage", statusmessage);
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            makingrecipe = tree.GetString("makingrecipe");
            recipefinishedat = tree.GetDouble("recipefinishedat");
            statusmessage = tree.GetString("statusmessage");
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            
            base.GetBlockInfo(forPlayer, dsc);
            dsc.AppendLine("recipe: " + makingrecipe);
            if (makingrecipe != null && makingrecipe != "") ;
            {
                double countdown = recipefinishedat - Api.World.Calendar.TotalHours;
                countdown = Math.Floor(countdown);
                dsc.AppendLine(countdown.ToString());
            }
            dsc.AppendLine(statusmessage);
        }

    }
    class MachineRecipe
    {
        public string name;
        public Dictionary<string, double> processingsteps;
        public double processinghours;
        public MachineRecipeItems[] ingredients;
        public MachineRecipeItems[] output;
        public MachineRecipe() { }
    }
    class MachineRecipeItems
    {
        public string[] validitems;
        public int quantity;
        public MachineRecipeItems() { }
    }
    
}