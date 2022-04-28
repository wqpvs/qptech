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
using Vintagestory.API.Client;
namespace qptech.src
{
    /// <summary>
    /// Creates clay items
    /// </summary>
    class BEEClayFormer:BEEBaseDevice
    {
        //TODO - some refactoring
        
        string currentrecipecode;
        public ClayFormingRecipe CurrentRecipe {
            get {
                
                if (Api!=null)
                {
                    return GetRecipeForItem(Api, currentrecipecode);
                }
                return null;
            }
            
        }
        int currentRecipeCost=0; //how much clay is needed (cache for answer)
        int CurrentRecipeCost {
            get {
                if (currentRecipeCost > 0) { return currentRecipeCost; }
                if (CurrentRecipe == null) { return 0; }
                currentRecipeCost = ClayCost(CurrentRecipe);
                return currentRecipeCost;
            }
        }
        BlockFacing rmInputFace;
        BlockFacing fgOutputFace;
        
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (Block.Attributes == null) { return; }
            rmInputFace = BlockFacing.FromCode(Block.Attributes["inputFace"].AsString("up"));
            fgOutputFace = BlockFacing.FromCode(Block.Attributes["outputFace"].AsString("down"));
            rmInputFace = OrientFace(Block.Code.ToString(), rmInputFace);
            fgOutputFace = OrientFace(Block.Code.ToString(), fgOutputFace);
        }

        protected override void DoDeviceStart()
        {
            if (!IsPowered) { deviceState= enDeviceState.POWERHOLD; return; }
            
            if (CurrentRecipe==null)
            {
                deviceState = enDeviceState.IDLE;

            }
            else
            {
                bool canstart = TryTakeMaterials();
                if (canstart)
                {
                    deviceState = enDeviceState.RUNNING;
                    starttime = Api.World.Calendar.TotalHours;
                    
                    return;
                }
                else
                {
                    deviceState = enDeviceState.MATERIALHOLD;
                    return;
                }
            }
            
           
        }
        
        
        protected override double completetime => starttime + processingTime * CurrentRecipeCost;
        protected override void UsePower()
        {
            
            if (!IsOn) { return; }
            if (deviceState == enDeviceState.POWERHOLD && IsPowered) { deviceState = enDeviceState.IDLE;MarkDirty();return; }
            if (deviceState == enDeviceState.WARMUP) { deviceState = enDeviceState.IDLE;MarkDirty();return; }
            if (deviceState == enDeviceState.IDLE) { DoDeviceStart();return; }
            if (deviceState == enDeviceState.MATERIALHOLD&&CurrentRecipe!=null&&TryTakeMaterials()) {
                deviceState = enDeviceState.RUNNING;
                currentRecipeCost = ClayCost(CurrentRecipe);
                starttime = Api.World.Calendar.TotalHours;
                MarkDirty();
                return;
            }
            if ((deviceState== enDeviceState.RUNNING&&Api.World.Calendar.TotalHours > completetime) || deviceState==enDeviceState.WAITOUTPUT) {
                DoDeviceComplete();
            }
        }
        
        /// <summary>
        /// Attempts to add item to production queue if it's valid
        /// </summary>
        /// <param name="toitem">Code to try to produce </param>
        public void SetCurrentItem(string toitem)
        {
            //if (Api is ICoreClientAPI) {  return; }
            if (deviceState == enDeviceState.RUNNING||deviceState==enDeviceState.WAITOUTPUT)
            {
                PlaySound(Api, "sounds/error", Pos);
                return;
            }
            currentrecipecode = toitem;
            
            currentRecipeCost = ClayCost(CurrentRecipe);
            PlaySound(Api, "sounds/filterset", Pos);
            return;
        }

        public void HaltProduction()
        {
            currentrecipecode = "";
            currentRecipeCost = 0;
            deviceState = enDeviceState.WARMUP;
            PlaySound(Api, "sounds/clearfilter", Pos);
        }

        protected override void DoDeviceComplete()
        {
            if (TryOutputProduct()) { deviceState = enDeviceState.IDLE; }
            else { deviceState = enDeviceState.WAITOUTPUT; }
        }

        //Attempt to take materials from the rmInputFace and returns true if succeeded
        bool TryTakeMaterials()
        {
            if (CurrentRecipe == null) { return false; }
            BlockPos checkpos = Pos.Copy().Offset(rmInputFace);
            //Check for a container at the rminputpos
            var checkblock = Api.World.BlockAccessor.GetBlockEntity(checkpos) as IBlockEntityContainer;
            if (checkblock == null) { return false; }
            if (checkblock.Inventory.Empty) { return false; }
            currentRecipeCost = ClayCost(CurrentRecipe); //just makes doubly sure we have right requirement
            int clayavailable = 0;
            
            foreach (ItemSlot slot in checkblock.Inventory)
            {
                if (slot == null || slot.Itemstack == null || slot.Empty) { continue; }
                bool goodslot = false;
                if (CurrentRecipe.Ingredient.SatisfiesAsIngredient(slot.Itemstack)) { goodslot = true; }
                else //if it doesn't work then try and find an alternate recipe
                {
                    ClayFormingRecipe alternaterecipe = GetRecipeForItemWithIngredient(Api,currentrecipecode, slot.Itemstack.Collectible.Code.ToString());
                    if (alternaterecipe != null)
                    {
                        goodslot = true;
                    }
                }
                if (goodslot)
                {
                    clayavailable += slot.Itemstack.StackSize;
                    
                }
                
                
                if (clayavailable >= currentRecipeCost) { break; }
            }
            if (clayavailable < currentRecipeCost) { return false; }
            //now that we have enough we need to take the clay
            int clayremaining = currentRecipeCost;
            foreach (ItemSlot slot in checkblock.Inventory)
            {
                if (slot == null || slot.Itemstack == null || slot.Empty) { continue; }
                bool goodslot = false;

                if (CurrentRecipe.Ingredient.SatisfiesAsIngredient(slot.Itemstack)) { goodslot = true; }
                else //if it doesn't work then try and find an alternate recipe
                {
                    ClayFormingRecipe alternaterecipe = GetRecipeForItemWithIngredient(Api, currentrecipecode, slot.Itemstack.Collectible.Code.ToString());
                    if (alternaterecipe != null)
                    {
                        goodslot = true;
                    }
                }

                if (goodslot){
                    int takeclay = Math.Min(clayremaining, slot.Itemstack.StackSize);
                    slot.Itemstack.StackSize -= takeclay;
                    if (slot.Itemstack.StackSize <= 0) { slot.Itemstack = null; }
                    slot.MarkDirty();
                    clayremaining -= takeclay;
                    if (clayremaining <= 0) { break; }
                }
            }
            
            return true;
        }

        //attempt to push finished product into the appropriate container or return false
        bool TryOutputProduct()
        {
            if (CurrentRecipe == null) { return true; } //really this should be an error, but in this case it will reset the machine back to idle
            BlockPos checkpos = Pos.Copy().Offset(fgOutputFace);
            var checkblock = Api.World.BlockAccessor.GetBlockEntity(checkpos) as IBlockEntityContainer;
            if (checkblock == null) { return false; }
            DummyInventory di = new DummyInventory(Api,1);
            di[0].Itemstack = CurrentRecipe.Output.ResolvedItemstack.Clone();
            
            foreach (ItemSlot slot in checkblock.Inventory)
            {
                int originalmaount = di[0].StackSize;
                int moved = di[0].TryPutInto(Api.World, slot,di[0].StackSize);
                if (moved == originalmaount)
                {
                    slot.MarkDirty();
                    PlaySound(Api, "sounds/doorslide", Pos);
                    return true;
                }
            }
            return false;
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            if (CurrentRecipe != null) { tree.SetString("currentRecipe",currentrecipecode); }
            
        }
    
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            currentrecipecode = tree.GetString("currentRecipe", "");
            
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if (deviceState == enDeviceState.RUNNING)
            {
                // started at 5 complete at 10 takes 
                double totaltime = completetime - starttime;
                double elapsedtime = Api.World.Calendar.TotalHours - starttime;
                if (totaltime>0.1)
                {
                    double pct = (elapsedtime) / totaltime;
                    int percent = (int)(pct * 100);
                    if (percent > 0)
                    {
                        dsc.AppendLine(percent + "% complete");
                    }
                }
            }
        }
        
        /// <summary>
        /// Return a clayforming recipe for a given item code (or null)
        /// </summary>
        /// <param name="Api"></param>
        /// <param name="clayformableitem">A string containing the Item Code eg "game:clayoven-north"</param>
        /// <returns>null if not found, or the matching clayforming recipe</returns>
        public static ClayFormingRecipe GetRecipeForItem(ICoreAPI Api, string clayformableitem)
        {
            if (Api == null) { return null; }
            

            List<ClayFormingRecipe> clayform = Api.GetClayformingRecipes();
            ClayFormingRecipe foundRecipe = clayform.FirstOrDefault(x => x.Output.Code.ToString() == clayformableitem);
            return foundRecipe;
        }
        
        /// <summary>
        /// Calculate the clay cost (by counting voxels) of a clayforming recipe
        /// </summary>
        /// <param name="forrecipe">A ClayFormingRecipe</param>
        /// <returns>-1 if null, or the amount of clay required to make it</returns>
        public static int ClayCost(ClayFormingRecipe forrecipe)
        {
            if (forrecipe == null) { return -1; }
            int usedvoxels = 0;
            foreach (bool ba in forrecipe.Voxels)
            {
                if (ba) { usedvoxels++; }
            }
            return usedvoxels/25;
        }
        
        public static ClayFormingRecipe GetRecipeForItemWithIngredient(ICoreAPI Api, string clayformableitem, string ingredient)
        {
            if (Api == null) { return null; }
            List<ClayFormingRecipe> clayform = Api.GetClayformingRecipes();
            ClayFormingRecipe foundRecipe = clayform.FirstOrDefault(x => x.Output.Code.ToString() == clayformableitem&&x.Ingredient.Code.ToString()==ingredient);
            return foundRecipe;
            
        }

        
    }
}
