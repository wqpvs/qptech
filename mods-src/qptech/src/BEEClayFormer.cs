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
using Vintagestory.API.Server;
using Vintagestory.API.Client;
namespace qptech.src
{
    /// <summary>
    /// Creates clay items
    /// </summary>
    class BEEClayFormer:BEEBaseDevice
    {
        
        
        
        ClayFormingRecipe currentRecipe;
        public ClayFormingRecipe CurrentRecipe => currentRecipe;
        int currentRecipeCost=0; //how much clay is needed (cache for answer)
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
            if (!IsPowered) { deviceState = enDeviceState.POWERHOLD; return; }
            
            if (currentRecipe==null)
            {
                deviceState = enDeviceState.IDLE;

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
            
           
        }

        bool IsComplete => true;

        protected override void UsePower()
        {
            
            if (!IsOn) { return; }
            if (deviceState == enDeviceState.WARMUP) { deviceState = enDeviceState.IDLE;MarkDirty();return; }
            if (deviceState == enDeviceState.IDLE) { DoDeviceStart();return; }
            if (deviceState == enDeviceState.MATERIALHOLD&&currentRecipe!=null&&TryTakeMaterials()) { deviceState = enDeviceState.RUNNING;MarkDirty(); return; }
            if (deviceState== enDeviceState.RUNNING||deviceState==enDeviceState.WAITOUTPUT && IsComplete) { DoDeviceComplete(); }
        }
        
        /// <summary>
        /// Attempts to add item to production queue if it's valid
        /// </summary>
        /// <param name="toitem">Code to try to produce </param>
        public void SetCurrentItem(string toitem)
        {
            if (Api is ICoreClientAPI) { return; }
            currentRecipe = GetRecipeForItem(Api, toitem);
            if (currentRecipe == null) { return; }
            currentRecipeCost = ClayCost(currentRecipe);
            return;
        }

        protected override void DoDeviceComplete()
        {
            if (TryOutputProduct()) { deviceState = enDeviceState.IDLE; }
            else { deviceState = enDeviceState.WAITOUTPUT; }
        }

        //Attempt to take materials from the rmInputFace and returns true if succeeded
        bool TryTakeMaterials()
        {
            if (currentRecipe == null) { return false; }
            BlockPos checkpos = Pos.Copy().Offset(rmInputFace);
            //Check for a container at the rminputpos
            var checkblock = Api.World.BlockAccessor.GetBlockEntity(checkpos) as IBlockEntityContainer;
            if (checkblock == null) { return false; }
            if (checkblock.Inventory.Empty) { return false; }
            currentRecipeCost = ClayCost(currentRecipe); //just makes doubly sure we have right requirement
            int clayavailable = 0;
            
            foreach (ItemSlot slot in checkblock.Inventory)
            {
                if (slot == null || slot.Itemstack == null || slot.Empty) { continue; }
                if (currentRecipe.Ingredient.SatisfiesAsIngredient(slot.Itemstack))
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
                if (currentRecipe.Ingredient.SatisfiesAsIngredient(slot.Itemstack))
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

        //attempt to push finished product into the appropriate container or return false
        bool TryOutputProduct()
        {
            if (currentRecipe == null) { return true; } //really this should be an error, but in this case it will reset the machine back to idle
            BlockPos checkpos = Pos.Copy().Offset(fgOutputFace);
            var checkblock = Api.World.BlockAccessor.GetBlockEntity(checkpos) as IBlockEntityContainer;
            if (checkblock == null) { return false; }
            DummyInventory di = new DummyInventory(Api,1);
            di[0].Itemstack = currentRecipe.Output.ResolvedItemstack.Clone();
            
            foreach (ItemSlot slot in checkblock.Inventory)
            {
                int originalmaount = di[0].StackSize;
                int moved = di[0].TryPutInto(Api.World, slot);
                if (moved == originalmaount)
                {
                    slot.MarkDirty();
                    return true;
                }
            }
            return false;
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
