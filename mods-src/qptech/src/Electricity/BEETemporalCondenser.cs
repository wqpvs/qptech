using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Server;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;
using qptech.src.extensions;
using System.Text.RegularExpressions;
using qptech.src.itemtransport;

namespace qptech.src
{
    /// <summary>
    /// The Temporal Condenser uses electricity and temporal instability to charge relvant materials (mainly temporal steel)
    /// </summary>
    class BEETemporalCondenser:BEElectric
    {
        float chargereq = 10000;
        ItemStack contents;
        
        
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            tempStabilitySystem = api.ModLoader.GetModSystem<SystemTemporalStability>();
            
        }

        public override void OnTick(float par)
        {
            base.OnTick(par);
            if (IsPowered && Api is ICoreServerAPI)
            {
                TryCharge();
            }
        }
        SystemTemporalStability tempStabilitySystem;
        public virtual void TryCharge()
        {
            if (tempStabilitySystem == null) { return; }
            float stability=tempStabilitySystem.GetTemporalStability(Pos);
            if (stability > 0.9f) { return; }
            float stabbonus = Math.Min(1, 1 - stability)*10;
            //TODO: add bonuses for nearby rifts? spawn rifts on transform?
            
            if (contents==null|| contents.StackSize == 0) { return; }
            float requiredcharge = contents.Collectible.Attributes["temporalCharge"].AsFloat(0);
            //if this isn't a chargeable object return;
            if (requiredcharge == 0) { return; }
            string temporalTransformBlockOrItem = contents.Attributes.GetString("temporalTransformBlockOrItem", "item");
            float currentcharge = contents.Attributes.GetFloat("temporalcharge", 0);
            string transformsto = contents.Collectible.Attributes["temporalTransformTo"].AsString("");
            currentcharge += stabbonus / (float)contents.StackSize;

            if (currentcharge >= requiredcharge) { 
             
                int qty = contents.StackSize;
                if (temporalTransformBlockOrItem == "item")
                {
                    Item newitem = Api.World.GetItem(new AssetLocation(transformsto));
                    ItemStack newstack = new ItemStack(newitem, qty);
                    contents = newstack;
                    //TODO UPDATE RENDER MESH
                }
                else
                {
                    Block newblock = Api.World.GetBlock(new AssetLocation(transformsto));
                    ItemStack newstack = new ItemStack(newblock, qty);
                    contents = newstack;
                    //TODO UPDATE RENDER MESH
                }
               
            }
            //item is not charged save the value
            else
            {
                contents.Attributes.SetFloat("temporalcharge", currentcharge);

            }
      
        }

        public virtual bool PlayerClicked(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            //if (capi != null) { return true; }

            //Pull an item from the players hand if applicable
            //TODO: Proper item transfer
            if ((contents == null || contents.StackSize == 0)&&(byPlayer.Entity.RightHandItemSlot.Itemstack!=null&&byPlayer.Entity.RightHandItemSlot.Itemstack.StackSize>0))
            {
                
                
                float requirecharge= byPlayer.Entity.RightHandItemSlot.Itemstack.Collectible.Attributes["temporalCharge"].AsFloat(0);
                if (requirecharge == 0) { return false; }
                contents = new ItemStack(byPlayer.Entity.RightHandItemSlot.Itemstack.Collectible, 1);
                float copycharge = byPlayer.Entity.RightHandItemSlot.Itemstack.Attributes.GetFloat("temporalcharge", 0);
                if (copycharge != 0) { contents.Attributes.SetFloat("temporalcharge", copycharge); }
                byPlayer.Entity.RightHandItemSlot.Itemstack.StackSize--;
                if (byPlayer.Entity.RightHandItemSlot.Itemstack.StackSize == 0) { byPlayer.Entity.RightHandItemSlot.Itemstack = null; }
                byPlayer.Entity.RightHandItemSlot.MarkDirty();
                //TODO Update renderer!
                MarkDirty(true);
                return true;
            }
            //Otherwise give item to player
            else if (contents != null && contents.StackSize > 0 && byPlayer.Entity.RightHandItemSlot.Empty)
            {
                byPlayer.Entity.RightHandItemSlot.Itemstack = new ItemStack(contents.Collectible, contents.StackSize);
                float copycharge = contents.Attributes.GetFloat("temporalcharge", 0);
                if (copycharge != 0) { byPlayer.Entity.RightHandItemSlot.Itemstack.Attributes.SetFloat("temporalcharge", copycharge); }
                contents = null;
                byPlayer.Entity.RightHandItemSlot.MarkDirty();
                MarkDirty(true);
                return true;
            }
            return false;
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            float stability = tempStabilitySystem.GetTemporalStability(Pos);
            if (contents != null && contents.StackSize > 0)
            {
                float requiredcharge = contents.Collectible.Attributes["temporalCharge"].AsFloat(0);
                
                float currentcharge = contents.Attributes.GetFloat("temporalcharge", 0);
                dsc.AppendLine("Loaded item: " + contents.Collectible.GetHeldItemName(contents));
                if (requiredcharge > 0)
                {
                    float pct = (float)Math.Ceiling(100*(currentcharge / requiredcharge) );
                    dsc.AppendLine("Charging progress " + pct + "%");
                }
                else
                {
                    dsc.AppendLine("Charging Complete!");
                }
            }
            if (stability > 0.9f) { dsc.AppendLine("Insufficent Temporal Instability ("+stability+")"); }
            else { dsc.AppendLine("Temporal Instability Adequate ("+stability+")"); }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            
            base.ToTreeAttributes(tree);
            tree.SetItemstack("contents", contents);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            contents = tree.GetItemstack("contents");
            if (contents != null)
            {
                contents.ResolveBlockOrItem(worldAccessForResolve);
            }
        }
    }
}
