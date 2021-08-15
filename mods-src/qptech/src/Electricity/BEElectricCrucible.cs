using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Client;

namespace qptech.src
{
    class BEElectricCrucible : BEElectric, IConduit
    {
        int tankCapacity = 1000;         //total storage
        float maxHeat = 2000;              //how hot can it go
        float minHeat = 20;
        float internalHeat = 20;          //current heat of everything (added items will instantly average their heat)
        int heatPerTickPerLiter = 200;    //how quickly it can heat it contents
        int heatLossPerTickPerLiter = 1; //how fast to cool contents if not heating
        int fluxPerTick = 1;           //how much power to use
        
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
        string currentMetalRecipe = "copper";
        Dictionary<string, int> storage; //track internal metals
        public enum enStatus { READY,HEATING,PRODUCING,CONSTRUCTION}
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            storage = new Dictionary<string, int>();
            status = enStatus.READY; //TODO REMOVE THIS LINE AFTER TESTING!
        }

        public override void OnTick(float par)
        {
            base.OnTick(par);
            if (status == enStatus.CONSTRUCTION) { CheckConstruction();return; }
            CheckInventories();
            DoStorageHeat();
            if (status == enStatus.PRODUCING) { DoProduction(); }
        }
        void CheckConstruction()
        {
            //TODO Check multiblock structre and do setups as necessary
        }
        void CheckInventories()
        {
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
                int used=ReceiveItemOffer(checkslot.Itemstack);
                //if (checkslot.Itemstack.StackSize == 0) { checkslot.Itemstack = new ItemStack(); }
            }
        }
        void DoStorageHeat()
        {
            if (UsedStorage == 0) { return; }
            
            //TODO Heat if powered, or cool storage as necessary
            if (IsOn&&Capacitor >= fluxPerTick)
            {
                ChangeCapacitor(-fluxPerTick);
                internalHeat +=  (heatPerTickPerLiter / (float)UsedStorage);
                internalHeat = Math.Min(internalHeat, maxHeat);
                return;
            }
            internalHeat-=(heatPerTickPerLiter / (float)UsedStorage);
            internalHeat = Math.Max(internalHeat, minHeat);
        }
        void DoProduction()
        {
            //TODO if set to produce, check if anything can be made
            //     - make and distribute it
            if (status != enStatus.PRODUCING) { return; }
        }

        public int ReceiveItemOffer(ItemStack offerstack, BlockFacing onFace)
        {
            //TODO Add facing check (?)
            return ReceiveItemOffer(offerstack);
        }
        public int ReceiveItemOffer(ItemStack offerstack)
        {
            //ingot = 20
            //nugget =5
            //todo - add a hatch part that will transmit these offers to the main device, the main device needs to tell hatch it is part of the multiblock
            //maybe add special liquid metal transfer stuff?
            if (offerstack == null) { return 0; }
            if (offerstack.StackSize == 0) { return 0; }
            if (status == enStatus.CONSTRUCTION) { return 0; }//under construction can't do anything yet
            if (FreeStorage == 0) { return 0; }
            if (offerstack.Item == null) { return 0; }
            if (!offerstack.Item.Code.ToString().Contains("ingot") && !offerstack.Item.Code.ToString().Contains("nugget")) { return 0; }
            //now we know we have nuggets and ingots, find the metal
            //List<AlloyRecipe> alloys = Api.World.Alloys;
            if (offerstack.Collectible.CombustibleProps == null) { return 0; }
            AssetLocation inass = offerstack.Collectible.CombustibleProps.SmeltedStack.Code; //heeheehee
            string inmetal = Api.World.GetItem(inass).LastCodePart();//heeheehee

            int multiplier = 5;
            if (offerstack.Item.LastCodePart().ToString() == "ingot") { multiplier = 20; }
            float intemp = offerstack.Collectible.GetTemperature(Api.World, offerstack);
            
            int internalused = offerstack.StackSize * multiplier;
            int returnused = offerstack.StackSize;
            internalHeat = (Math.Max(UsedStorage * internalHeat,minHeat) + internalused*intemp) / (UsedStorage + internalused);//average out the heat
            if (!storage.ContainsKey(inmetal)) { storage[inmetal] = internalused; }
            else { storage[inmetal] += internalused; }
            offerstack.StackSize -= returnused;
            
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
            foreach (string metal in storage.Keys)
            {
                dsc.AppendLine(metal + " " + storage[metal] + " units");
            }

        }
        //TODO: Control UI

        //TODO: TreeAttributes

        //TODO: Start Production, Output Items, Return to Idle status (locks out UI while producing?)

        //TODO: Manually check inventories on tick
        //TODO: Add player sneak click to add items manually - though possibly use hatch instead of main item?

    }
}
