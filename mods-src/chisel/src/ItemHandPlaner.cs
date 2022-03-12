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
    /// <summary>
    /// The HandPlaner will add or remove 1 voxel deep planes to chiseled objects (adding or shaving off a full sheet of voxels)
    /// </summary>
    class ItemHandPlaner:Item
    {
        int cutsize = 1; //for now we'll just leave it at 1x1 voxels, might change in future
        int cutdepth = 0; //this was meant to be a counter, but right now is just used locally in each function
       
        //These will track the last thing the planer clicked on
        BlockPos lastpos;
        BlockFacing lastfacing=BlockFacing.DOWN;
        
        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            if (blockSel == null) { return; }
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
            if (blockSel == null) { return; }
            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            {
                byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
                return;
            }

            if (byPlayer.Entity.Controls.Sneak)
            {
                ExtrudeAdd(blockSel);
            }
            else
            {
                PlaneAdd(blockSel);
            }
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
            CuboidWithMaterial cwm = new CuboidWithMaterial();
            if (bmb.VoxelCuboids == null || bmb.VoxelCuboids.Count == 0)
            {
                if (!(api is ICoreClientAPI)) { api.World.BlockAccessor.SetBlock(0, blockSel.Position);return; }
                else { return; }
            }
            

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
            CuboidWithMaterial cwm = new CuboidWithMaterial();
            //convert the hit point into voxel coordinates
            Vec3i s = new Vec3i((int)(blockSel.HitPosition.X * 16f), (int)(blockSel.HitPosition.Y * 16f), (int)(blockSel.HitPosition.Z * 16f));
            if (blockSel.Face == BlockFacing.SOUTH) { s.Z--; }
            if (blockSel.Face == BlockFacing.UP) { s.Y--; }
            if (blockSel.Face == BlockFacing.EAST) { s.X--; }

            foreach (uint voxint in bmb.VoxelCuboids)
            {
                //this static method converts the uint into a CubioidWithMaterial
                BlockEntityMicroBlock.FromUint(voxint, cwm);
                //check if the voxel we are looking at is part of the cubiod
                if (!cwm.Contains(s.X, s.Y, s.Z)) { continue; }
                //if it's part of the cuboid grab its material to use
                useindex = cwm.Material;
                break;
            }
            bool state = true;
            //loop thru 16 x 16 voxel plane (xc/yc will be swapped with other coordinates depending on the direction we are facing)
            //tell the microblock to add each voxel
            for (int xc = 0; xc < 16 / cutsize; xc++)
            {
                for (int yc = 0; yc < 16 / cutsize; yc++)
                {
                    if (lastfacing == BlockFacing.NORTH)
                    {
                        cutdepth = (int)(blockSel.HitPosition.Z * 16)-1;
                        if (cutdepth > 15||cutdepth<0) { return; }
                        bmb.SetVoxel(new Vec3i(xc * cutsize, yc * cutsize, cutdepth * cutsize), state, null, useindex, cutsize);
                    }
                    else if (lastfacing == BlockFacing.SOUTH)
                    {
                        cutdepth = (int)(blockSel.HitPosition.Z * 16) - cutsize+1;
                        if (cutdepth > 15) { return; }
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
            


        }

        //this function will ultimately (hopefully) extrude only relevant faces eg: instead of adding an entire plane, if you had 2x2 voxels sticking out it would extrude only those
        public virtual void ExtrudeAdd(BlockSelection blockSel)
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
            if (bmb.VoxelCuboids == null | bmb.VoxelCuboids.Count == 0) { return; }
            CuboidWithMaterial cwm = new CuboidWithMaterial();
            Vec3i s = new Vec3i((int)(blockSel.HitPosition.X * 16f), (int)(blockSel.HitPosition.Y * 16f), (int)(blockSel.HitPosition.Z * 16f));
            byte useindex = 0;
            foreach (uint voxint in bmb.VoxelCuboids)
            {
                BlockEntityMicroBlock.FromUint(voxint, cwm);
                if (!cwm.ContainsOrTouches(s.X,s.Y,s.Z)) { continue; }
                useindex = cwm.Material;
                break;
            }
                       
            bool state = true;
            bmb.MarkDirty(true);
            


        }

    }
}
