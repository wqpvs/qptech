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


namespace chiseltools
{
    class ChiselToolLoader : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterItemClass("ItemHandPlaner", typeof(ItemHandPlaner));
        }
    }
    class ItemHandPlaner:Item
    {
        int cutsize = 1;
        int cutdepth = 0;
       
        
        BlockPos lastpos;
        BlockFacing lastfacing=BlockFacing.DOWN;
        //maybe store a list of previous worked faces? Hmmm


        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            {
                byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
                return;
            }
            PlaneCut(blockSel);
            handling = EnumHandHandling.PreventDefaultAction;
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            {
                byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
                return;
            }

            PlaneAdd(blockSel);

            handling = EnumHandHandling.PreventDefaultAction;
        }
        public virtual void PlaneCut(BlockSelection blockSel)
        {
            if (blockSel == null)
            {
                lastpos = null; return;
            }
            if (lastpos == null || lastpos != blockSel.Position || lastfacing != blockSel.Face)
            {
                lastpos = blockSel.Position;
                lastfacing = blockSel.Face;
                
            }
            BlockEntityMicroBlock bmb = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityMicroBlock;
            if (bmb == null) { lastpos = null;  return; }
            for (int xc = 0; xc < 16 / cutsize; xc++)
            {
                for (int yc = 0; yc < 16 / cutsize; yc++)
                {
                    if (lastfacing == BlockFacing.NORTH)
                    {
                        cutdepth = (int)(blockSel.HitPosition.Z * 16);
                        //bmb.SetVoxel(new Vec3i(xc * cutsize, yc * cutsize, cutcounter * cutsize), false, null, 0, cutsize);
                        bmb.SetVoxel(new Vec3i(xc * cutsize, yc * cutsize, cutdepth * cutsize), false, null, 0, cutsize);
                    }
                    else if (lastfacing == BlockFacing.SOUTH)
                    {
                        cutdepth = (int)(blockSel.HitPosition.Z * 16) - cutsize;
                        //bmb.SetVoxel(new Vec3i(xc * cutsize, yc * cutsize, (16-cutsize)-cutcounter * cutsize), false, null, 0, cutsize);
                        bmb.SetVoxel(new Vec3i(xc * cutsize, yc * cutsize, cutdepth * cutsize), false, null, 0, cutsize);
                    }
                    else if (lastfacing == BlockFacing.WEST)
                    {
                        cutdepth = (int)(blockSel.HitPosition.X * 16);
                        bmb.SetVoxel(new Vec3i(cutdepth * cutsize, yc * cutsize, xc * cutsize), false, null, 0, cutsize);
                    }
                    else if (lastfacing == BlockFacing.EAST)
                    {
                        cutdepth = (int)(blockSel.HitPosition.X * 16) - cutsize;
                        bmb.SetVoxel(new Vec3i(cutdepth * cutsize, yc * cutsize, xc * cutsize), false, null, 0, cutsize);
                    }
                    else if (lastfacing == BlockFacing.DOWN)
                    {
                        cutdepth = (int)(blockSel.HitPosition.Y * 16);
                        bmb.SetVoxel(new Vec3i(yc * cutsize, cutdepth * cutsize, xc * cutsize), false, null, 0, cutsize);
                    }
                    else if (lastfacing == BlockFacing.UP)
                    {
                        cutdepth = (int)(blockSel.HitPosition.Y * 16) - cutsize;
                        bmb.SetVoxel(new Vec3i(yc * cutsize, cutdepth * cutsize, xc * cutsize), false, null, 0, cutsize);
                    }
                }
            }
            bmb.MarkDirty(true);
            cutdepth++;
            if (cutdepth * cutsize >= 16 - cutsize) { cutdepth = 0; }
            

        }
        public virtual void PlaneAdd(BlockSelection blockSel)
        {
            if (blockSel == null)
            {
                lastpos = null; return;
            }
            if (lastpos == null || lastpos != blockSel.Position || lastfacing != blockSel.Face)
            {
                lastpos = blockSel.Position;
                lastfacing = blockSel.Face;

            }
            BlockEntityMicroBlock bmb = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityMicroBlock;
            if (bmb == null) { lastpos = null; return; }
            byte useindex = 0;
            bool state = true;
            for (int xc = 0; xc < 16 / cutsize; xc++)
            {
                for (int yc = 0; yc < 16 / cutsize; yc++)
                {
                    if (lastfacing == BlockFacing.NORTH)
                    {
                        cutdepth = (int)(blockSel.HitPosition.Z * 16)-1;
                        if (cutdepth > 15||cutdepth<0) { return; }
                        //bmb.SetVoxel(new Vec3i(xc * cutsize, yc * cutsize, cutcounter * cutsize), false, null, 0, cutsize);
                        bmb.SetVoxel(new Vec3i(xc * cutsize, yc * cutsize, cutdepth * cutsize), state, null, useindex, cutsize);
                    }
                    else if (lastfacing == BlockFacing.SOUTH)
                    {
                        cutdepth = (int)(blockSel.HitPosition.Z * 16) - cutsize+1;
                        if (cutdepth > 15) { return; }
                        //bmb.SetVoxel(new Vec3i(xc * cutsize, yc * cutsize, (16-cutsize)-cutcounter * cutsize), false, null, 0, cutsize);
                        bmb.SetVoxel(new Vec3i(xc * cutsize, yc * cutsize, cutdepth * cutsize), state, null, useindex, cutsize);
                    }
                    else if (lastfacing == BlockFacing.WEST)
                    {
                        cutdepth = (int)(blockSel.HitPosition.X * 16)-1;
                        if (cutdepth > 15 || cutdepth < 0) { return; }
                        bmb.SetVoxel(new Vec3i(cutdepth * cutsize, yc * cutsize, xc * cutsize), state, null, useindex, cutsize);
                    }
                    else if (lastfacing == BlockFacing.EAST)
                    {
                        cutdepth = (int)(blockSel.HitPosition.X * 16) - cutsize + 1;
                            if (cutdepth > 15) { return; };
                        bmb.SetVoxel(new Vec3i(cutdepth * cutsize, yc * cutsize, xc * cutsize), state, null, useindex, cutsize);
                    }
                    else if (lastfacing == BlockFacing.DOWN)
                    {
                        cutdepth = (int)(blockSel.HitPosition.Y * 16)-1;
                        if (cutdepth > 15 || cutdepth < 0) { return; }
                        bmb.SetVoxel(new Vec3i(yc * cutsize, cutdepth * cutsize, xc * cutsize), state, null, useindex, cutsize);
                    }
                    else if (lastfacing == BlockFacing.UP)
                    {
                        cutdepth = (int)(blockSel.HitPosition.Y * 16) - cutsize+1;
                        if (cutdepth > 15) { return; }
                        bmb.SetVoxel(new Vec3i(yc * cutsize, cutdepth * cutsize, xc * cutsize), state, null, useindex, cutsize);
                    }
                }
            }
            bmb.MarkDirty(true);
            cutdepth++;
            if (cutdepth * cutsize >= 16 - cutsize) { cutdepth = 0; }


        }
    }
}
