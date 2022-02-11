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
        public string StatusMessage => statusmessage;
        public List<MachineRecipe> Recipes => recipes;
        public string MakingRecipe => makingrecipe;



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
                        foreach (string re in recipegroups)
                        {
                            if (mr.name.Contains(re))
                            {
                                recipes.Add(mr);break;
                            }
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
                    recipefinishedat = Api.World.ElapsedMilliseconds + 100;MarkDirty();
                }
                
                return;
            }

            enDeviceState ogdevicestate = deviceState;
            if (deviceState == enDeviceState.ERROR)
            {
                deviceState = enDeviceState.IDLE;
            }
            else if (makingrecipe == "")
            {
                DoDeviceStart();
            }
            else if (Api.World.ElapsedMilliseconds >= recipefinishedat)
            {
                DoDeviceComplete();
            }
            else if (Api.World.ElapsedMilliseconds < recipefinishedat)
            {
                if (!CheckProcessing()){
                    recipefinishedat = Api.World.ElapsedMilliseconds + 250; 
                    deviceState = enDeviceState.PROCESSHOLD;
                    MarkDirty();
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
            if (makingrecipe == "") { return false; }
            if (deviceState == enDeviceState.IDLE||deviceState==enDeviceState.WARMUP||deviceState==enDeviceState.MATERIALHOLD) { return false; }
            MachineRecipe mr = recipes.Find(m => m.name == makingrecipe);
            if (mr == null) { return false; }
            if (mr.processingsteps==null||mr.processingsteps.Count == 0) { return true; }
            
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
            
            if (recipes is null || recipes.Count == 0) { deviceState = enDeviceState.ERROR; return; }
            //Check for ingredients
            Dictionary<string, int> availableingredients= new Dictionary<string, int>();
            AddAdditionalIngredients(ref availableingredients);
            InventoryBase checkinv=CheckContainer(ref availableingredients);

            MachineRecipe canmake = null;
            foreach (MachineRecipe mr in recipes)
            {
                bool ok = true;
                foreach (MachineRecipeItems mi in mr.ingredients)
                {
                    int foundcount = 0;
                    string matched = mi.MatchAny(availableingredients.Keys.ToList<string>());
                    if (matched!="")
                    {
                        foundcount += availableingredients[matched];
                        
                    }
                    
                    if (foundcount < mi.quantity) { ok = false; break; }
                }
                if (!ok) { continue; }
                else { canmake = mr;break; }
            }
            if (canmake == null) { return; }
            
            // If we can make something extract the ingredients

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
                    if (!mi.Match(checkcode)) { continue; }
                    int maxtake = Math.Min(countdown, checkstack.StackSize);
                    checkstack.StackSize -= maxtake;
                    countdown -= maxtake;
                    if (checkstack.StackSize <= 0) { checkslot.Itemstack = null; }
                    checkslot.MarkDirty();
                    if (countdown <= 0) { break; }
                }
                if (countdown > 0) { countdown -= TryAdditionalDraw(mi,countdown); }
            }


            //Start Processing if all is good, set state
            makingrecipe = canmake.name;
            recipefinishedat = Api.World.ElapsedMilliseconds + canmake.processingTime;
            deviceState = enDeviceState.RUNNING;
        }
        protected virtual int TryAdditionalDraw(MachineRecipeItems mi, int needqty)
        {
            return 0;
        }
        protected virtual void AddAdditionalIngredients(ref Dictionary<string, int> ingredientlist)
        {
            
            return;
        }

        protected virtual InventoryBase CheckContainer(ref Dictionary<string, int> availableingredients)
        {
            BlockFacing checkface = matInputFace;
            BlockPos checkpos = Pos.Copy().Offset(checkface);

            BlockEntity checkbe = Api.World.BlockAccessor.GetBlockEntity(checkpos);
            if (checkbe == null) { return null; }
            BlockEntityContainer checkcont = checkbe as BlockEntityContainer;
            if (checkcont == null) { return null; }
            InventoryBase checkinv = checkcont.Inventory;
            if (checkinv == null || checkinv.Empty) { return null; }

            //add up all available ingredients
            foreach (ItemSlot checkslot in checkinv)
            {
                if (checkslot == null || checkslot.Empty) { continue; }
                ItemStack checkstack = checkslot.Itemstack;
                if (checkstack == null | checkstack.StackSize == 0 || (checkstack.Item == null && checkstack.Block == null)) { continue; }
                string checkcode = "";

                if (checkstack.Item != null) { checkcode = checkstack.Item.Code.ToString(); }
                else { checkcode = checkstack.Block.Code.ToString(); }
                if (!availableingredients.ContainsKey(checkcode)) { availableingredients[checkcode] = checkstack.StackSize; }
                else { availableingredients[checkcode] += checkstack.StackSize; }
            }
            return checkinv;
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
                if (mr.processingsteps!=null&&mr.processingsteps.ContainsKey("heating"))
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
            ResetTimers();
            makingrecipe = "";
        }
        /*GUIRecipeProcessorStatus gas;
        public override void OpenStatusGUI()
        {
            ICoreClientAPI capi = Api as ICoreClientAPI;
            if (capi != null)
            {
                if (gas == null)
                {
                    gas = new GUIRecipeProcessorStatus("Processor Status", Pos, capi);

                    gas.TryOpen();
                    gas.SetupDialog(this);

                }
                else
                {
                    gas.TryClose();
                    gas.TryOpen();
                    gas.SetupDialog(this);
                }
            }

        }*/
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
            
            dsc.AppendLine(statusmessage);
        }
        public override string GetStatusUI()
        {
            string statustext = "<font color=\"#ffffff\">";
            if (MakingRecipe != "") { statustext += "<strong>CURRENT RECIPE: " + MakingRecipe + "</strong></font><br><br>"; }
            if (StatusMessage != "")
            {
                statustext += StatusMessage + "<br>";
            }

            if (Recipes != null && Recipes.Count > 0)
            {
                statustext += "<strong>Available Recipes:</strong><br>";
                foreach (MachineRecipe mr in Recipes)
                {
                    statustext += "<br><font color=\"#aaffaa\">";
                    statustext += "<strong>";// +mr.name ;
                    //statustext += " makes ";
                    foreach (MachineRecipeItems mri in mr.output)
                    {
                        statustext += mri.quantity + " ";
                        int vi = mri.validitems.Count();
                        if (vi > 1) { statustext += "("; }
                        int c = 1;
                        foreach (string subi in mri.validitems)
                        {

                            AssetLocation al = new AssetLocation(subi);
                            string usestring = Lang.Get(al.Path);


                            statustext += usestring;

                            if (vi > 1 && c == vi - 1) { statustext += " or "; }
                            else if (vi > 1 && c != vi) { statustext += ","; }
                            c++;
                        }
                        if (vi > 1) { statustext += ")"; }

                    }
                    if (Recipes.Count < 3)
                    {
                        statustext += " from</strong></font><br><font color=\"#ffffff\">";
                        foreach (MachineRecipeItems mri in mr.ingredients)
                        {
                            statustext += "   " + mri.quantity + " ";
                            int vi = mri.validitems.Count();
                            if (vi > 1) { statustext += "("; }
                            int c = 1;
                            foreach (string subi in mri.validitems)
                            {
                                AssetLocation al = new AssetLocation(subi);
                                string usestring = Lang.Get(al.Path);


                                statustext += usestring;
                                if (vi > 1 && c == vi - 1) { statustext += " or "; }
                                else if (vi > 1 && c != vi) { statustext += ","; }
                                c++;
                            }
                            if (vi > 1) { statustext += ")"; }
                            statustext += "<br>";
                        }
                        if (mr.processingsteps != null && mr.processingsteps.Count() > 0)
                        {
                            statustext += "  *Requires ";
                            int c = 1;
                            foreach (string key in mr.processingsteps.Keys)
                            {
                                statustext += key + "(" + mr.processingsteps[key] + ")";
                                if (c < mr.processingsteps.Count()) { statustext += ", "; }
                                c++;
                            }
                        }
                    }
                }
                statustext += "</font></strong>";

            }
            return statustext+ base.GetStatusUI();
        }
    }
    class MachineRecipe
    {
        public string name;
        public Dictionary<string, double> processingsteps;
        public double processingTime;
        public MachineRecipeItems[] ingredients;
        public MachineRecipeItems[] output;
        public MachineWildCard[] wildcards;
        public MachineRecipe() { }
        
    }
    class MachineRecipeItems
    {
        public string[] validitems;
        public int quantity;
        public MachineRecipeItems() { }
        public bool Match(string tryitem)
        {
            if (validitems == null || validitems.Length == 0) { return false; }
            if (validitems.Contains(tryitem)) { return true; }
            for (int c= 0;c < validitems.Length; c++){
                
                if (tryitem.Contains(validitems[c])){ return true; }
            }
            return false;
        }
        public string MatchAny(List<string> tryitems)
        {
            foreach (string tryitem in tryitems)
            {
                if (Match(tryitem)) { return tryitem; }
            }
            return "";
        }
    }
    class MachineWildCard
    {
        public string wildcard;
        public string[] variants;
    }
}
