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
using Vintagestory.GameContent;


namespace qptech.src.misc
{
    class ItemHandPlaner:Item
    {
        int cutsize = 1;
        int cutcounter = 0;
        BlockPos lastpos;
        BlockFacing lastfacing=BlockFacing.DOWN;
        //maybe store a list of previous worked faces? Hmmm
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (blockSel == null)
            {
                lastpos = null; handling = EnumHandHandling.NotHandled; return;
            }
            if (lastpos == null || lastpos != blockSel.Position || lastfacing!=blockSel.Face)
            {
                lastpos = blockSel.Position;
                lastfacing = blockSel.Face;
                cutcounter = 0;
            }
            //base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            BlockEntityMicroBlock bmb = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityMicroBlock;
            
            
            if (bmb == null) { lastpos = null; handling = EnumHandHandling.NotHandled;return; }
            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            for (int xc = 0; xc < 16 / cutsize; xc++)
            {
                for (int yc = 0; yc < 16 / cutsize; yc++)
                {
                    if (lastfacing == BlockFacing.NORTH)
                    {
                        cutcounter = (int)(blockSel.HitPosition.Z * 16);
                        //bmb.SetVoxel(new Vec3i(xc * cutsize, yc * cutsize, cutcounter * cutsize), false, null, 0, cutsize);
                        bmb.SetVoxel(new Vec3i(xc * cutsize, yc * cutsize, cutcounter * cutsize), false, null, 0, cutsize);
                    }
                    else if (lastfacing == BlockFacing.SOUTH)
                    {
                        cutcounter = (int)(blockSel.HitPosition.Z * 16)-cutsize;
                        //bmb.SetVoxel(new Vec3i(xc * cutsize, yc * cutsize, (16-cutsize)-cutcounter * cutsize), false, null, 0, cutsize);
                        bmb.SetVoxel(new Vec3i(xc * cutsize, yc * cutsize, cutcounter * cutsize), false, null, 0, cutsize);
                    }
                    else if (lastfacing == BlockFacing.WEST)
                    {
                        cutcounter = (int)(blockSel.HitPosition.X * 16);
                        bmb.SetVoxel(new Vec3i(cutcounter * cutsize, yc * cutsize, xc * cutsize), false, null, 0, cutsize);
                    }
                    else if (lastfacing == BlockFacing.EAST)
                    {
                        cutcounter = (int)(blockSel.HitPosition.X * 16)-cutsize;
                        bmb.SetVoxel(new Vec3i(cutcounter * cutsize, yc * cutsize, xc * cutsize), false, null, 0, cutsize);
                    }
                    else if (lastfacing == BlockFacing.DOWN)
                    {
                        cutcounter = (int)(blockSel.HitPosition.Y * 16);
                        bmb.SetVoxel(new Vec3i( yc * cutsize, cutcounter * cutsize, xc * cutsize), false, null, 0, cutsize);
                    }
                    else if (lastfacing == BlockFacing.UP)
                    {
                        cutcounter = (int)(blockSel.HitPosition.Y * 16)-cutsize;
                        bmb.SetVoxel(new Vec3i(yc * cutsize, cutcounter * cutsize, xc * cutsize), false, null, 0, cutsize);
                    }
                }
            }
            bmb.MarkDirty(true);
            cutcounter++;
            if (cutcounter * cutsize >= 16-cutsize) { cutcounter = 0; }
            handling = EnumHandHandling.Handled;

        }
    }
}
