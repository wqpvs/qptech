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
        
        
        Queue<string> productionQueue;
        ClayFormingRecipe currentRecipe;
        int currentRecipeCost=0; //how much clay is needed (cache for answer)
        BlockFacing rmInputFace;
        BlockFacing fgOutputFace;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            productionQueue = new Queue<string>();
            if (Block.Attributes == null) { return; }
            rmInputFace = BlockFacing.FromCode(Block.Attributes["inputFace"].AsString("up"));
            fgOutputFace = BlockFacing.FromCode(Block.Attributes["outputFace"].AsString("down"));
            rmInputFace = OrientFace(Block.Code.ToString(), rmInputFace);
            fgOutputFace = OrientFace(Block.Code.ToString(), fgOutputFace);
        }

        protected override void DoDeviceStart()
        {
            if (!IsPowered) { deviceState = enDeviceState.POWERHOLD; return; }
            
            if (currentRecipe==null && productionQueue.Count > 0)
            {
                bool founditem=SetNextItem();
                if (founditem)
                {
                    deviceState = enDeviceState.RUNNING;
                    MarkDirty();
                    return;
                }

            }
            else
            {
                bool canstart = TryTakeMaterials();
                if (canstart)
                {
                    deviceState = enDeviceState.RUNNING;
                    return;
                }
                else
                {
                    deviceState = enDeviceState.MATERIALHOLD;
                    return;
                }
            }
            base.DoDeviceStart();
           
        }
        protected override void UsePower()
        {
            if (deviceState==enDeviceState.WARMUP) { deviceState = enDeviceState.IDLE;return; }
            if (deviceState == enDeviceState.IDLE) { DoDeviceStart(); }
            if (deviceState == enDeviceState.MATERIALHOLD&&TryTakeMaterials()) { deviceState = enDeviceState.RUNNING;return; }

        }
        /// <summary>
        /// Check the production queue for another item, set currentItem and return true if new item was found
        /// </summary>
        /// <returns></returns>
        public bool SetNextItem()
        {
            if (productionQueue == null || productionQueue.Count == 0) { return false; }
            string currentBlockCode = productionQueue.Dequeue();
            currentRecipe = GetRecipeForItem(Api,currentBlockCode);
            if (currentRecipe == null) { return false; }
            currentRecipeCost = ClayCost(currentRecipe);
            return true;
        }

        /// <summary>
        /// Attempts to add item to production queue if it's valid
        /// </summary>
        /// <param name="toitem">Code to try to produce </param>
        public void SetCurrentItem(string toitem)
        {
            if (Api is ICoreClientAPI) { return; }
            productionQueue.Enqueue(toitem);
            currentRecipe = GetRecipeForItem(Api, toitem);
            if (currentRecipe == null) { return; }
            currentRecipeCost = ClayCost(currentRecipe);
            MarkDirty();
            return;
        }
        //Attempt to take materials from the rmInputFace and returns true if succeeded
        bool TryTakeMaterials()
        {
            if (currentRecipe == null) { return false; }
            BlockPos checkpos = Pos.Copy().Offset(rmInputFace);
            //Check for a container at the rminputpos
            var checkblock = Api.World.BlockAccessor.GetBlockEntity(checkpos) as BlockEntityContainer;
            if (checkblock == null) { return false; }
            if (checkblock.Inventory.Empty) { return false; }
            currentRecipeCost = ClayCost(currentRecipe); //just makes doubly sure we have right requirement
            int clayavailable = 0;
            foreach (ItemSlot slot in checkblock.Inventory)
            {
                if (slot == null || slot.Itemstack == null || slot.Empty) { continue; }
                if (slot.Itemstack.Collectible.Code == currentRecipe.Ingredient.Code)
                {
                    clayavailable += slot.Itemstack.StackSize;
                    if (clayavailable >= currentRecipeCost) { break; }
                }
            }
            if (clayavailable < currentRecipeCost) { return false; }
            //now that we have enough we need to take the clay
            int clayremaining = currentRecipeCost;
            foreach (ItemSlot slot in checkblock.Inventory)
            {
                if (slot == null || slot.Itemstack == null || slot.Empty) { continue; }
                if (slot.Itemstack.Collectible.Code == currentRecipe.Ingredient.Code)
                {
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

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            if (currentRecipe != null) { tree.SetString("currentRecipe",currentRecipe.Output.Code.ToString()); }
            else { tree.SetString("currentRecipe", ""); }
        }
    
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            string currentrecipecode = tree.GetString("currentRecipe", "");
            currentRecipe = GetRecipeForItem(Api,currentrecipecode);
            currentRecipeCost = ClayCost(currentRecipe);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if (currentRecipe != null) { dsc.AppendLine("Current Recipe:" + currentRecipe.Output.Code.ToShortString()+" "+currentRecipeCost+" units of "+currentRecipe.Ingredient.Code.ToString()); }
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
            AssetLocation currental = new AssetLocation(clayformableitem);

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
    }
}
