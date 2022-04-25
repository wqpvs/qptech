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
        string testitem = "game:clayoven-north";
        bool teststarted = false;
        Queue<string> productionQueue;
        ClayFormingRecipe currentRecipe;
        int currentRecipeCost=0; //how much clay is needed (cache for answer)
        

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            productionQueue = new Queue<string>();
            if (!teststarted)
            {
                for (int c = 0; c < 5; c++) { productionQueue.Enqueue(testitem); }
                teststarted = true;
            }
        }

        protected override void DoDeviceStart()
        {
            if (currentRecipe==null && productionQueue.Count > 0&&IsPowered)
            {
                bool founditem=SetNextItem();
                if (founditem)
                {
                    deviceState = enDeviceState.RUNNING;
                    MarkDirty();
                }
            }
            base.DoDeviceStart();
           
        }

        /// <summary>
        /// Check the production queue for another item, set currentItem and return true if new item was found
        /// </summary>
        /// <returns></returns>
        bool SetNextItem()
        {
            if (productionQueue == null || productionQueue.Count == 0) { return false; }
            string currentBlockCode = productionQueue.Dequeue();
            currentRecipe = GetRecipeForItem(Api,currentBlockCode);
            if (currentRecipe == null) { return false; }
            currentRecipeCost = ClayCost(currentRecipe);
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
        /// <param name="clayformableitem">A string containing the Item Code</param>
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
