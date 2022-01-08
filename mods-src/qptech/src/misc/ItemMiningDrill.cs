using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace qptech.src.misc
{
    class ItemMiningDrill:Item
    {
        float nextactionat = 0;
        bool soundplayed = false;
        float actionspeed = 1f;
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            //base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            nextactionat = actionspeed;


            handling = EnumHandHandling.Handled;
        }
        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            // - start particles
            // - first extension
            // - break top/bottom/side blocks (maybe add calculations to make it faster for less blocks
            // - second extension
            // - break back block
            // - reset
            if (blockSel == null) { return false; }
            //if (!BlockFacing.HORIZONTALS.Contains(blockSel.Face)) { return false; } //not pointed at a block ahead, cancel
            if (secondsUsed > 0.25f && !soundplayed)
            {
                //api.World.PlaySoundAt(new AssetLocation("sounds/quarrytemp"), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, null, false, 8, 1);
                soundplayed = true;
            }
            if (secondsUsed > nextactionat)
            {

                IPlayer p = api.World.NearestPlayer(byEntity.Pos.X, byEntity.Pos.Y, byEntity.Pos.Z);
                Block tb;
                
                List<BlockPos> positions = new List<BlockPos>();
                
                int sx = 0; int ex = 0;
                int sy = 0; int ey = 0;
                int sz = 0; int ez = 0;
                if (blockSel.Face == BlockFacing.UP||blockSel.Face==BlockFacing.DOWN)
                {
                    sx = -1;ex = 1;sz = -1;ez = +1;
                }
                else if (blockSel.Face == BlockFacing.EAST || blockSel.Face == BlockFacing.WEST)
                {
                    sy = -1;ey = 1;
                    sz = -1;ez = 1;
                }
                else if (blockSel.Face == BlockFacing.NORTH || blockSel.Face == BlockFacing.SOUTH)
                {
                    sy = -1;ey = 1;
                    sx = -1;ex = 1;
                }
                
                for (int xc = sx; xc < ex+1; xc++)
                {
                    for (int zc = sz; zc < ez + 1; zc++)
                    {
                        for (int yc = sy; yc < ey + 1; yc++)
                        {
                            BlockPos newpos = blockSel.Position.Copy();
                            newpos.X += xc;
                            newpos.Y += yc;
                            newpos.Z += zc;
                            positions.Add(newpos);
                        }
                    }
                }

                foreach (BlockPos bp in positions)
                {
                    
                    tb = api.World.BlockAccessor.GetBlock(bp);
                    if (tb == null) { continue; }
                    if (tb.MatterState != EnumMatterState.Solid) { continue; }
                    if (tb.RequiredMiningTier > 5) { continue; }
                    tb.OnBlockBroken(api.World, bp, p, 1);
                
                    
                }
                nextactionat += actionspeed;
            }
            return true;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            soundplayed = false;
            nextactionat = 0;
            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);
        }

    }
}

