using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Newtonsoft.Json;

namespace qptech.src
{
    class BEElectricCrucible : BEElectric, IConduit
    {
        int tankCapacity = 25600;         //total storage
        float maxHeat = 2000;              //how hot can it go
        float minHeat = 20;
        float internalHeat = 20;          //current heat of everything (added items will instantly average their heat)
        int heatPerTickPerLiter = 25000;    //how quickly it can heat it contents
        int heatLossPerTickPerLiter = 10; //how fast to cool contents if not heating
        int fluxPerTick = 1;           //how much power to use
        int ingotsize = 100;
        public int FreeStorage
        {
            get
            {
                if (storage == null) { return 0; }
                return tankCapacity - UsedStorage;
            }
        }
        public int UsedStorage
        {
            get
            {
                if (storage == null) { return 0; }
                return storage.Values.Sum();
            }
        }

        public bool Full => UsedStorage == FreeStorage;

        enStatus status = enStatus.CONSTRUCTION;
        string currentMetalRecipe = "";
        int currentOrder = 0;
        Dictionary<string, int> storage; //track internal metals
        public Dictionary<string, int> recipes;
        public enum enStatus { READY,HEATING,PRODUCING,CONSTRUCTION}
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            
            status = enStatus.READY; //TODO REMOVE THIS LINE AFTER TESTING!
        }

        public override void OnTick(float par)
        {
            base.OnTick(par);
            //TEMP CODE TO MAKE COPPER
            if (!(Api is ICoreServerAPI)) { return; }
            if (storage!=null&& storage.ContainsKey("copper") && storage["copper"] >= 100 && currentOrder == 0)
            {
                currentOrder = storage["copper"] / 100;
                currentMetalRecipe = "copper";
            }
            if (status == enStatus.CONSTRUCTION) { CheckConstruction();return; }
            
            CheckInventories();
            DoStorageHeat();
            SetStatus();
            if (status == enStatus.PRODUCING) { DoProduction(); }
            MarkDirty(true);
        }

        void SetStatus()
        {
            if (internalHeat < maxHeat) { status = enStatus.HEATING; return; }
            if (currentOrder > 0 && currentMetalRecipe != "") { status = enStatus.PRODUCING; return; }
            status = enStatus.READY;
        }
        void CheckConstruction()
        {
           
        }
        
        void CheckInventories()
        {
            
            if (storage == null) { storage = new Dictionary<string, int>();return; }
            if (Full) { return; }
            
            //TODO Check appropriate containers for inventory
            BlockPos tempcheckpos = Pos.Copy().Up();
            BlockEntity checkbe = Api.World.BlockAccessor.GetBlockEntity(tempcheckpos);
            var inputContainer = checkbe as BlockEntityContainer;
            if (inputContainer == null) { return; }
            if (inputContainer.Inventory.Empty) { return; }
            for (int c = 0; c < inputContainer.Inventory.Count; c++)
            {
                if (Full) { return; }
                ItemSlot checkslot = inputContainer.Inventory[c];
                if (checkslot.Itemstack == null) { continue; }
                if (checkslot.StackSize == 0) { continue; }
                int used=ReceiveItemOffer(checkslot);
                if (used > 0)
                {
                    
                    checkbe.MarkDirty(true);
                   // MarkDirty(true);
                }
                //if (checkslot.Itemstack.StackSize == 0) { checkslot.Itemstack = new ItemStack(); }
            }
            
        }
        void DoStorageHeat()
        {
            //if (Api is ICoreClientAPI) { return; }
            if (UsedStorage == 0) { return; }
            
            //TODO Heat if powered, or cool storage as necessary
            if (IsOn&&Capacitor >= fluxPerTick)
            {
                ChangeCapacitor(-fluxPerTick);
                internalHeat +=  (heatPerTickPerLiter / (float)UsedStorage);
                internalHeat = Math.Min(internalHeat, maxHeat);
                return;
            }
            internalHeat-=(heatLossPerTickPerLiter / (float)UsedStorage);
            internalHeat = Math.Max(internalHeat, minHeat);
            
            //MarkDirty(true);
        }
        void DoProduction()
        {
            //TODO if set to produce, check if anything can be made
            //     - make and distribute it
            if (recipes == null) { return; }
            if (status != enStatus.PRODUCING) { return; } //should probably pre check this
            if (currentOrder == 0 || currentMetalRecipe == "") { HaltProduction();return; } //problem with order, or it's done - reset status
            if (!recipes.ContainsKey(currentMetalRecipe)) { HaltProduction();return; } //recipe doesn't exist - reset status
            if (recipes[currentMetalRecipe] < 1) { return; } //Can't make any of order right now, do nothing
            
                MakeOne(); //We should be able to make part of order, do so
            
        }
        void HaltProduction()
        {
            currentMetalRecipe = "";
            currentOrder = 0;
            status = enStatus.READY;
        }

        void MakeOne()
        {
            //first check if we have the materials to directly make ingot
            if (storage.ContainsKey(currentMetalRecipe))
            {
                if (storage[currentMetalRecipe] >=ingotsize) //enough material to make an ingot
                {
                    AssetLocation al = new AssetLocation("game:ingot-" + currentMetalRecipe);
                    //TODO Will have to check for ingots in other domains?
                    Item makeitem=Api.World.GetItem(al);
                    
                    //Try to make the item
                    if (CreateItem(makeitem, 1)) {
                        currentOrder--;
                        storage[currentMetalRecipe] =storage[currentMetalRecipe]- ingotsize; //take metals out of storage
                        FindValidRecipes();
                        
                    }
                    else { HaltProduction(); }
                    //MarkDirty(true);
                    return;
                }
            }

            //next see if there's an alloy recipe matching what we're trying to make
            AlloyRecipe ar = Api.World.Alloys.Where(i => Api.World.GetItem(i.Output.Code).LastCodePart() == currentMetalRecipe) as AlloyRecipe;
            if (ar == null) { HaltProduction(); return; }
        }
        /// <summary>
        /// actually create the ingot or other item, return false if something went wrong
        /// </summary>
        /// <param name="makeitem">Item To Make</param>
        /// <param name="makeqty">Quantity To Make</param>
        /// <returns>true if everything ok, false if there's an issue</returns>
        bool CreateItem(Item makeitem,int makeqty)
        {
            if (makeitem == null) { return false; }
            //if (!(Api is ICoreClientAPI))
            //{
                DummyInventory di = new DummyInventory(Api, 1);
                di[0].Itemstack = new ItemStack(makeitem, makeqty);
                di[0].Itemstack.Collectible.SetTemperature(Api.World,di[0].Itemstack,internalHeat);
                
                Vec3d pos = Pos.ToVec3d();

                di.DropAll(pos);
            //}
            return true;
        }

        public int ReceiveItemOffer(ItemSlot offerslot, BlockFacing onFace)
        {
            //TODO Add facing check (?)
            return ReceiveItemOffer(offerslot);
        }
        public int ReceiveItemOffer(ItemSlot offerslot)
        {
            //ingot = 20
            //nugget =5
            //todo - add a hatch part that will transmit these offers to the main device, the main device needs to tell hatch it is part of the multiblock
            //maybe add special liquid metal transfer stuff?
            if (offerslot == null) { return 0; }
            if (offerslot.StackSize == 0) { return 0; }
            if (status == enStatus.CONSTRUCTION) { return 0; }//under construction can't do anything yet
            if (FreeStorage == 0) { return 0; }
            if (offerslot.Itemstack.Item == null) { return 0; }
            if (!offerslot.Itemstack.Item.Code.ToString().Contains("ingot") && !offerslot.Itemstack.Item.Code.ToString().Contains("nugget") && !offerslot.Itemstack.Item.Code.ToString().Contains("bit")) { return 0; }
            //now we know we have nuggets and ingots, find the metal
            //List<AlloyRecipe> alloys = Api.World.Alloys;
            string inmetal = Api.World.GetItem(offerslot.Itemstack.Item.Code).LastCodePart();
            if (offerslot.Itemstack.Collectible.CombustibleProps != null)
            {
                AssetLocation inass = offerslot.Itemstack.Collectible.CombustibleProps.SmeltedStack.Code; //heeheehee
                inmetal = Api.World.GetItem(inass).LastCodePart();//heeheee
            }
            int multiplier = 5;
            if (offerslot.Itemstack.Item.Code.ToString().Contains("ingot")) { multiplier = ingotsize; }
            float intemp = offerslot.Itemstack.Collectible.GetTemperature(Api.World, offerslot.Itemstack);
            
            int internalused = offerslot.Itemstack.StackSize * multiplier;
            int returnused = offerslot.Itemstack.StackSize;
            internalHeat = (Math.Max(UsedStorage * internalHeat,minHeat) + internalused*intemp) / (UsedStorage + internalused);//average out the heat
            if (!storage.ContainsKey(inmetal)) { storage[inmetal] = 0; }
            storage[inmetal] += internalused; 
            offerslot.TakeOut(returnused);
            FindValidRecipes();
            return returnused;
        }

        //TODO: Display inventory
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if (status == enStatus.CONSTRUCTION) { dsc.AppendLine("STRUCTURE INCOMPLETE"); return; }
            if (UsedStorage == 0) { dsc.AppendLine("EMPTY"); return; }
            dsc.AppendLine(UsedStorage + " units used out of " + tankCapacity);
            dsc.AppendLine("Heated to " + internalHeat + "C");
            dsc.AppendLine("--------");
            dsc.AppendLine("CAN MAKE THESE INGOTS");
            foreach (string metal in recipes.Keys)
            {
                dsc.AppendLine(recipes[metal]+" "+metal+" ingots");
            }
            dsc.AppendLine("------");
            foreach (string key in storage.Keys)
            {
                dsc.AppendLine(storage[key] + " units of " + key);
            }
            if (currentOrder > 0)
            {
                dsc.AppendLine("------");
                dsc.AppendLine(status.ToString());
                dsc.AppendLine("Production order " + currentOrder + " of " + currentMetalRecipe);
            }
        }
        //TODO: Control UI

        //TODO: TreeAttributes
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);


            //if (type == null) type = defaultType; // No idea why. Somewhere something has no type. Probably some worldgen ruins
            //capacitor = tree.GetInt("capacitor");
            internalHeat=tree.GetFloat("internalHeat");
            currentOrder = tree.GetInt("currentOrder", 0);
            currentMetalRecipe = tree.GetString("currentMetalRecipe");
            if (!tree.HasAttribute("storage")) { storage = new Dictionary<string, int>(); return; }
            var asString = tree.GetString("storage");
            if (asString != "")
            {
                try
                {
                    storage = JsonConvert.DeserializeObject<Dictionary<string, int>>(asString);
                }
                catch
                {
                    storage = new Dictionary<string, int>();
                }
            }
            string statstring = tree.GetString("status");
            Enum.TryParse(statstring, out enStatus status);
            FindValidRecipes();
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            //tree.SetInt("capacitor", Capacitor);
            tree.SetFloat("internalHeat",internalHeat);
            tree.SetString("currentMetalRecipe", currentMetalRecipe);
            tree.SetInt("currentOrder", currentOrder);
            var asString = JsonConvert.SerializeObject(storage);
            tree.SetString("storage", asString);
            tree.SetString("status", status.ToString());
        }
        //TODO: Start Production, Output Items, Return to Idle status (locks out UI while producing?)
                
        //TODO: Add player sneak click to add items manually - though possibly use hatch instead of main item?

        void FindValidRecipes()
        {
            if (Api == null) { return; }
            recipes = new Dictionary<string, int>();
            if (storage == null || storage.Count == 0) { return; }
            //First just add the basic ingot recipes (need 20 units to make an ingot)
            foreach (string key in storage.Keys)
            {
                if (storage[key] <ingotsize) { continue; }
                recipes[key] = storage[key] / ingotsize;
            }
            foreach (AlloyRecipe ar in Api.World.Alloys)
            {
                float hasenoughfor = 0;
                foreach (MetalAlloyIngredient i in ar.Ingredients)
                {

                    string metal= Api.World.GetItem(i.Code).LastCodePart();
                    if (!storage.ContainsKey(metal)) { hasenoughfor = 0; break; }
                    
                    int enoughthisingredient= storage[metal]/(int)(i.MaxRatio*(float)ingotsize);
                    if (enoughthisingredient < 1) { hasenoughfor = 0;break; }
                    if (hasenoughfor == 0) { hasenoughfor = enoughthisingredient; }
                    hasenoughfor = Math.Min(hasenoughfor, enoughthisingredient);
                }
                
                if (hasenoughfor < 1) { continue; }
                string outmetal = Api.World.GetItem(ar.Output.Code).LastCodePart();
                recipes[outmetal] = (int)hasenoughfor;
                //TEMP CODE TO SET PRODUCTION:
                
            }
        }
    }
}
