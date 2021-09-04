using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Runtime;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using qptech.src.multiblock;
using Newtonsoft.Json;

namespace qptech.src
{
    public enum enProductionMode { ONE,REPEAT}
    class BEElectricCrucible : BEElectric, IFunctionalMultiblockMaster
    {
        int tankCapacity = 25600;         //total storage
        public int TotalStorage => tankCapacity;
        float maxHeat = 2000;              //how hot can it go
        float minHeat = 20;
        float internalHeat = 20;          //current heat of everything (added items will instantly average their heat)
        int heatPerTickPerLiter = 25000;    //how quickly it can heat it contents
        int heatLossPerTickPerLiter = 10; //how fast to cool contents if not heating
        int fluxPerTick = 1;           //how much power to use
        public int FluxPerTick => fluxPerTick;
        int ingotsize = 100;
        int uiUpdateEvery = 20;
        int uiUpdateSkipCounter = 0;
        double productionTime = 0.05;
        double nextproductionat = 0;
        enProductionMode mode = enProductionMode.ONE;
        public enProductionMode Mode => mode;
        Vintagestory.API.Common.Action<Block, BlockPos> OnInitializeMember => InitializeBlock;
        public float internalTempPercent => (internalHeat - minHeat) / (maxHeat - minHeat);
        public Dictionary<string, int> Recipes => recipes;

        public bool ActiveOrder => (currentOrder > 0 || mode == enProductionMode.REPEAT) && currentMetalRecipe != "";
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
        public enum enStatus { READY, HEATING, PRODUCING, CONSTRUCTION }
        enStatus status = enStatus.CONSTRUCTION;
        public enStatus Status => status;
        string currentMetalRecipe = "";
        int currentOrder = 0;
        Dictionary<string, int> storage; //track internal metals
        public Dictionary<string, int> Storage => storage;

        List<IFunctionalMultiblockPart> parts;
        public List<IFunctionalMultiblockPart> Parts { get { return parts; } }
        BlockPos mboffset;
        MultiblockStructure ms;
        public MultiblockStructure MBStructure { get { return ms; } }
        public BlockPos MBOffset { get { return mboffset; } }

        public string StructureName =>"Electric Crucible";

        public Dictionary<string, int> recipes;
        


        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            ms = Block.Attributes["multiblockStructure"].AsObject<MultiblockStructure>();
            int[] offsarray = { 0, 0, 0 };
            offsarray = Block.Attributes["mboffset"].AsArray<int>(offsarray);
            mboffset = new BlockPos(offsarray[0] + Pos.X, offsarray[1] + Pos.Y, offsarray[2] + Pos.Z);

            ms.InitForUse(0);
            status = enStatus.CONSTRUCTION; //TODO REMOVE THIS LINE AFTER TESTING!
        }

        public override void OnTick(float par)
        {
            base.OnTick(par);

            //Only process if multiblock is built
            if (Api is ICoreServerAPI)
            {
                if (status == enStatus.CONSTRUCTION) { CheckConstruction(); return; }

                ProcessBlocks(par);  //do ticks for the multiblocks
                CheckInventories();  //refresh inventory
                DoStorageHeat();     //heat things up if need & possible
                SetStatus();         //update status of block
                if (status == enStatus.PRODUCING) { DoProduction(); }
            }
            if (Api is ICoreClientAPI)
            {
                UpdateUI();
            }
        }

        void ProcessBlocks(float par)
        {
            if (parts==null || parts.Count == 0) { return; }
            foreach (IFunctionalMultiblockPart p in parts)
            {
                if (p != null) { p.OnPartTick(par); }
            }
        }
        void UpdateUI()
        {
            uiUpdateSkipCounter++;
            if (uiUpdateSkipCounter >= uiUpdateEvery)
            {
                uiUpdateSkipCounter = 0;
                if (gas != null && gas.IsOpened())
                {
                    gas.SetupDialog(this);
                }
            }
        }
        void SetStatus()
        {
            if (internalHeat < maxHeat * 0.95) { status = enStatus.HEATING; }
            else if (ActiveOrder) { status = enStatus.PRODUCING; }
            
            else { 
                if (status == enStatus.PRODUCING) { currentOrder = 0;currentMetalRecipe = ""; }
                status = enStatus.READY;
            }
        }
        void CheckConstruction()
        {
            if (CheckCompleteStructure()) {
                if (status == enStatus.CONSTRUCTION)
                {
                    InitializeCompletedStructure();
                }
                
            }
            else { status = enStatus.CONSTRUCTION; }
        }

        void InitializeCompletedStructure()
        {
            
            parts = new List<IFunctionalMultiblockPart>();
            ms.WalkMatchingBlocks(Api.World, mboffset,OnInitializeMember);
            status = enStatus.READY;
        }
        
        public void InitializeBlock(Block memberblock, BlockPos bp)
        {

            IFunctionalMultiblockPart part = Api.World.BlockAccessor.GetBlockEntity(bp) as IFunctionalMultiblockPart;
            if (part == null) { return; }
            parts.Add(part);
            part.InitializePart(this);
        }
        bool CheckCompleteStructure()
        {
            if (ms == null) { return false; }
            if (ms.InCompleteBlockCount(Api.World, mboffset) > 0)
            {

                return false;
            }

            return true;
        }
        void CheckInventories()
        {
            
            if (storage == null) { storage = new Dictionary<string, int>();return; }
            if (Full) { return; }
            if (ms == null) { return; }
            //Checking parts for input hatches
            foreach (IFunctionalMultiblockPart p in parts)
            {


                
                var hatch = p as MBItemHatch;
                if (hatch == null) { continue; }
                if (hatch.IsOutput) { continue; }
                var inputContainer = p as IBlockEntityContainer;
                if (inputContainer == null) { continue; }
                if (inputContainer.Inventory.Empty) { continue; }
                bool dirty = false;
                for (int c = 0; c < inputContainer.Inventory.Count; c++)
                {
                    if (Full) { break; }
                    ItemSlot checkslot = inputContainer.Inventory[c];
                    if (checkslot.Itemstack == null) { continue; }
                    if (checkslot.StackSize == 0) { continue; }
                    int used = ReceiveItemOffer(checkslot);
                    if (used > 0)
                    {
                        dirty = true;
                        // MarkDirty(true);
                    }
                    //if (checkslot.Itemstack.StackSize == 0) { checkslot.Itemstack = new ItemStack(); }
                }
                if (dirty) { MarkDirty(true); }
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
            if (recipes == null) {
                FindValidRecipes();
                if (recipes == null)
                {
                    return;
                }
            }
            if (!recipes.ContainsKey(currentMetalRecipe)) { HaltProduction();return; } //recipe doesn't exist - reset status
            if (recipes[currentMetalRecipe] < 1) { return; } //Can't make any of order right now, do nothing
            if (Api.World.Calendar == null) { return; }
            if (Api.World.Calendar.TotalHours > nextproductionat)
            {
                MakeOne(); //We should be able to make part of order, do so
            }
            
        }
        void HaltProduction()
        {
            currentMetalRecipe = "";
            currentOrder = 0;
            status = enStatus.READY;
        }

        void MakeOne()
        {
        
           Api.World.PlaySoundAt(new AssetLocation("sounds/outputchunk"), Pos.X, Pos.Y, Pos.Z, null, false, 8, 1);
            AssetLocation al=new AssetLocation("game:ingot-" + currentMetalRecipe); ;
            Item makeitem= Api.World.GetItem(al);
            nextproductionat = Api.World.Calendar.TotalHours + productionTime;
            if (storage.ContainsKey(currentMetalRecipe)&& storage[currentMetalRecipe] >= ingotsize)
            {

                if (CreateItem(makeitem, 1)) {
                    currentOrder--;
                    storage[currentMetalRecipe] =storage[currentMetalRecipe]- ingotsize; //take metals out of storage
                    FindValidRecipes();
                    MarkDirty(true);
                    return;
                }

            }

            //next see if there's an alloy recipe matching what we're trying to make
            AlloyRecipe ar=null;
            foreach (AlloyRecipe arc in Api.World.Alloys)
            {
                if (arc.Output.Code.ToString() == makeitem.Code.ToString()) { ar = arc; break; }
            }
            if (ar == null) {
                HaltProduction(); return; 
            }
            bool canmake = true;
            foreach (MetalAlloyIngredient mi in ar.Ingredients)
            {
                string ingmetal = Api.World.GetItem(mi.Code).LastCodePart();
                if (!storage.ContainsKey(ingmetal)) { canmake = false;break; }
                if (storage[ingmetal] < (int)(mi.MinRatio * (float)ingotsize)) { canmake = false;break; }
            }
            if (!canmake) { HaltProduction();return; }
            al = new AssetLocation("game:ingot-" + currentMetalRecipe);
            makeitem = Api.World.GetItem(al);
            
            if (CreateItem(makeitem, 1)) {
                foreach (MetalAlloyIngredient mi in ar.Ingredients)
                {
                    string ingmetal = Api.World.GetItem(mi.Code).LastCodePart();
                    storage[ingmetal] -= (int)(mi.MinRatio * (float)ingotsize);
                    
                }
                currentOrder--;
                
            }
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
            
            if (parts != null)
            {
                foreach (IFunctionalMultiblockPart p in parts)
                {
                    if (di[0].StackSize == 0) { break; }
                    var phatch = p as MBItemHatch;
                    if (phatch == null||phatch.IsInput) { continue; } //This isn't a hatch or this isn't an output hatch
                    var pc = p as IBlockEntityContainer;
                    if (pc == null) { continue; } //part isn't a container continue
                    foreach (ItemSlot slot in pc.Inventory)
                    {
                        if (!slot.CanHold(di[0])) { continue; }
                        ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, di[0].StackSize);

                        int taken=di[0].TryPutInto(slot, ref op);
                        
                    }
                }
            }
            //dump the left overs
            if (di[0].StackSize > 0)
            {

                Vec3d pos = Pos.SouthCopy().UpCopy().ToVec3d();
                di.DropAll(pos);
            }
            return true;
        }

        public int ReceiveItemOffer(ItemSlot offerslot, BlockFacing onFace)
        {
            //TODO Add facing check (?)
            return ReceiveItemOffer(offerslot);
        }
        public int ReceiveItemOffer(ItemSlot offerslot)
        {
           
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
            dsc.AppendLine(status.ToString());
            dsc.AppendLine(mode.ToString());
            dsc.AppendLine("Temp at " + internalHeat + "/" + maxHeat + "C");
            if (ActiveOrder) {
                dsc.AppendLine(currentMetalRecipe + " x" + currentOrder);
                dsc.AppendLine((nextproductionat+" (Current time: "+Api.World.Calendar.TotalHours+")").ToString());
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
            string modestring = tree.GetString("mode");
            Enum.TryParse(modestring, out enProductionMode mode);
            nextproductionat = tree.GetDouble("nextproductionat");
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
            tree.SetString("mode",mode.ToString());
            tree.SetDouble("nextproductionat", nextproductionat);
        }
        //TODO: Start Production, Output Items, Return to Idle status (locks out UI while producing?)
                
        //TODO: Add player sneak click to add items manually - though possibly use hatch instead of main item?

        void FindValidRecipes()
        {
            if (Api == null) { return; }
            recipes = new Dictionary<string, int>();
            if (storage == null || storage.Count == 0) { return; }
            List<string> emptykeys = new List<string>();
            //First just add the basic ingot recipes (need 20 units to make an ingot)
            foreach (string key in storage.Keys)
            {
                if (storage[key] == 0) { emptykeys.Add(key); continue; }
                if (storage[key] <ingotsize) { continue; }
                recipes[key] = storage[key] / ingotsize;
            }
            //remove storage listings for zero quantity items
            foreach (string key in emptykeys)
            {
                storage.Remove(key);
            }
            foreach (AlloyRecipe ar in Api.World.Alloys)
            {
                float hasenoughfor = 0;
                foreach (MetalAlloyIngredient i in ar.Ingredients)
                {

                    string metal= Api.World.GetItem(i.Code).LastCodePart();
                    if (!storage.ContainsKey(metal)) { hasenoughfor = 0; break; }
                    
                    int enoughthisingredient= storage[metal]/(int)(i.MinRatio*(float)ingotsize);
                    if (enoughthisingredient < 1) { hasenoughfor = 0;break; }
                    if (hasenoughfor == 0) { hasenoughfor = enoughthisingredient; }
                    hasenoughfor = Math.Min(hasenoughfor, enoughthisingredient);
                }
                
                if (hasenoughfor < 1) { continue; }
                string outmetal = Api.World.GetItem(ar.Output.Code).LastCodePart();
                if (recipes.ContainsKey(outmetal)) { recipes[outmetal] += (int)hasenoughfor; }
                else { recipes[outmetal] = (int)hasenoughfor; }
                //TEMP CODE TO SET PRODUCTION:
                
            }
        }

        GUIElectricCrucible gas;
        public void OpenStatusGUI()
        {
            ICoreClientAPI capi = Api as ICoreClientAPI;
            if (capi != null)
            {
                if (gas == null)
                {
                    gas = new GUIElectricCrucible("Crucible Status", Pos, capi);

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


        }


        public bool Interact(IPlayer byPlayer)
        {
            CheckConstruction();
            if (status != enStatus.CONSTRUCTION) { OpenStatusGUI(); }
            else {
                if (showingcontents)
                {
                    
                    ms.ClearHighlights(Api.World, byPlayer);
                    showingcontents = false;
                }
                ToggleBuildStatus(byPlayer);
            }
            return true;
        }
        bool showingcontents = false;
        void ToggleBuildStatus(IPlayer byPlayer)
        {
            showingcontents = !showingcontents;
            
            if (Api.Side == EnumAppSide.Client)
            {
                if (showingcontents)
                {
                    ms.HighlightIncompleteParts(Api.World, byPlayer, mboffset);
                }
                else
                {
                    ms.ClearHighlights(Api.World, byPlayer);
                }
            }
        }
        /// <summary>
        /// Set the order for this electric crucible
        /// </summary>
        /// <param name="formetal">what metal to make</param>
        /// <param name="qty">how many to make</param>
        /// <param name="onlycompleteorder">if true will only start production if order can be completed, if false will make what it can</param>
        /// <returns>Whether order can be delivered</returns>
        
        public enum enPacketIDs
        {
            SetOrder=99990001,
            TogglePower=99990002,
            ToggleMode=99990003,
            Halt=99990004
        }

        public void SetOrder(string formetal, int qty, bool onlycompleteorder)
        {
            ICoreClientAPI capi = Api as ICoreClientAPI;
            if (capi != null)
            {
                if (status != enStatus.READY) { return; }
                FindValidRecipes();
                if (!recipes.ContainsKey(formetal)) { return ; }
                if (recipes[formetal] < qty && onlycompleteorder) { return; }
                currentOrder = Math.Min(recipes[formetal], qty);
                currentMetalRecipe = formetal;
                Dictionary<string, int> neworder = new Dictionary<string, int>();
                neworder[formetal] = currentOrder;
                byte[] data = ObjectToByteArray(neworder);
                (Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, (int)enPacketIDs.SetOrder,data);
                if (Api.World.Calendar != null) { nextproductionat = Api.World.Calendar.TotalHours + productionTime; }
            }
            else if (Api is ICoreServerAPI)
            {
                if (status != enStatus.READY) { return; }
                FindValidRecipes();
                if (!recipes.ContainsKey(formetal)) { return; }
                if (recipes[formetal] < qty && onlycompleteorder) { return; }
                currentOrder = Math.Min(recipes[formetal], qty);
                currentMetalRecipe = formetal;
                status = enStatus.PRODUCING;
                if (Api.World.Calendar != null) { nextproductionat = Api.World.Calendar.TotalHours + productionTime; }
            }
            
        }
        public void ButtonTogglePower()
        {
            (Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, (int)enPacketIDs.TogglePower, null);
            TogglePower();
            UpdateUI();
        }
        public void ButtonToggleMode()
        {
            (Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, (int)enPacketIDs.ToggleMode, null);
            ToggleMode();
            UpdateUI();
        }

        public void HaltButton()
        {
            (Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, (int)enPacketIDs.Halt, null);
            //Add an alarm sound?
        }

        void ToggleMode()
        {
            if (mode == enProductionMode.ONE) { mode = enProductionMode.REPEAT; }
            else { mode = enProductionMode.ONE; }
        }
        public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
        
        {
            
            if (packetid == (int)enPacketIDs.SetOrder)
            {
                var inmetal = ByteArrayToObject(data) as Dictionary<string,int>;
                foreach (string key in inmetal.Keys)
                {
                    SetOrder(key, inmetal[key], true);
                }
                return;
            }
            if (packetid == (int)enPacketIDs.TogglePower)
            {
                isOn = !isOn;
                MarkDirty(true);
            }
            if (packetid == (int)enPacketIDs.ToggleMode)
            {
                ToggleMode();
                MarkDirty(true);
            }
            if (packetid == (int)enPacketIDs.Halt)
            {
                HaltProduction();
                
                MarkDirty(true);
                
            }
            base.OnReceivedServerPacket(packetid, data);
        }
        
        private byte[] ObjectToByteArray(Object obj)
        {
            if (obj == null)
                return null;

            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, obj);

            return ms.ToArray();
        }

        // Convert a byte array to an Object
        private Object ByteArrayToObject(byte[] arrBytes)
        {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            Object obj = (Object)binForm.Deserialize(memStream);

            return obj;
        }

        public string Making
        {
            get
            {
                if (currentOrder > 0 || mode == enProductionMode.REPEAT && IsOn)
                {
                    return currentMetalRecipe;
                }
                return "---";
            }
        }
    }
}
