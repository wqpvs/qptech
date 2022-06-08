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

namespace qptech.src
{
    class BEETemporalCondenser:BEElectric
    {
        float chargereq = 10000;
        
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
            bool changed = false;
            //TODO: add bonuses for nearby rifts? spawn rifts on transform?
            //check for containers with valid chargable items - eventually i'll add item attributes for this
            foreach (BlockFacing bf in BlockFacing.ALLFACES)
            {
                BlockPos checkpos = Pos.Copy().Offset(bf);
                BlockEntityContainer bec = Api.World.BlockAccessor.GetBlockEntity(checkpos) as BlockEntityContainer;
                if (bec == null) { continue; }
                if (bec.Inventory == null || bec.Inventory.Empty) { continue; }
                foreach (ItemSlot slot in bec.Inventory)
                {
                    if (slot == null || slot.Itemstack==null|| slot.Empty||slot.StackSize<1) { return; }
                    ItemStack stack = slot.Itemstack;
                    string transformsto = stack.Collectible.Attributes["temporalTransformTo"].AsString("");
                    if (transformsto == "") { continue; }
                    float requiredcharge = stack.Collectible.Attributes["temporalCharge"].AsFloat(10000);
                    float currentcharge = stack.Attributes.GetFloat("temporalcharge", 0);
                    string temporalTransformBlockOrItem = stack.Attributes.GetString("temporalTransformBlockOrItem", "item");
                    currentcharge += stabbonus/(float)stack.StackSize;
                    //item is charged do the transform
                    if (currentcharge > requiredcharge)
                    {
                        int qty = stack.StackSize;
                        if (temporalTransformBlockOrItem == "item")
                        {
                            Item newitem = Api.World.GetItem(new AssetLocation(transformsto));
                            ItemStack newstack = new ItemStack(newitem, qty);
                            slot.Itemstack = newstack;
                            
                        }
                        else
                        {
                            Block newblock = Api.World.GetBlock(new AssetLocation(transformsto));
                            ItemStack newstack = new ItemStack(newblock, qty);
                            slot.Itemstack = newstack;
                        }
                        slot.MarkDirty();
                        changed = true;
                        
                    }
                    //item is not charged save the value
                    else
                    {
                        stack.Attributes.SetFloat("temporalcharge", currentcharge);
                        slot.MarkDirty();
                        changed = true;
                    }
                }
                if (changed) { bec.MarkDirty(); }
            }
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            float stability = tempStabilitySystem.GetTemporalStability(Pos);
            if (stability > 0.9f) { dsc.AppendLine("Insufficent Temporal Instability ("+stability+")"); }
            else { dsc.AppendLine("Temporal Instability Adequate ("+stability+")"); }
        }
    }
}
