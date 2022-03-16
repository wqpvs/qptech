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
using Vintagestory.API.Server;


namespace chiseltools
{
    
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
            int cutvoxels = 0;
            if (byPlayer.Entity.Controls.Sneak)
            {
                cutvoxels=ExtrudeAdd(blockSel, false);
            }
            else
            {
                cutvoxels=PlaneCut(blockSel);
            }
            if (api is ICoreServerAPI)
            {
                if (byPlayer?.WorldData.CurrentGameMode == EnumGameMode.Creative)
                {
                    handling = EnumHandHandling.PreventDefaultAction;
                }
                else
                {
                    this.DamageItem(api.World, byEntity, byPlayer.InventoryManager.ActiveHotbarSlot, CalcDamage(cutvoxels));
                }
            }
            handling = EnumHandHandling.PreventDefaultAction;
        }

        protected virtual int CalcDamage(int numvoxels)
        {
            int basedamage = ChiselToolLoader.serverconfig.handPlanerBaseDurabilityUse * numvoxels;
            basedamage=(int)(ChiselToolLoader.serverconfig.handPlanerBaseDurabilityMultiplier*(float)numvoxels);
            if (basedamage < 0) { basedamage = 0; }
            return basedamage;
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
            int cutvoxels = 0;
            if (byPlayer.Entity.Controls.Sneak)
            {
                cutvoxels=ExtrudeAdd(blockSel,true);
            }
            else
            {
                cutvoxels = PlaneAdd(blockSel);
            }
            if (api is ICoreServerAPI)
            {
                this.DamageItem(api.World, byEntity, byPlayer.InventoryManager.ActiveHotbarSlot, CalcDamage(cutvoxels));
            }
            
            handling = EnumHandHandling.PreventDefaultAction;
        }
        public virtual int PlaneCut(BlockSelection blockSel)
        {
            if (blockSel == null)
            {
                lastpos = null; return 0;
            }
            if (lastpos == null || lastpos != blockSel.Position || lastfacing != blockSel.Face)
            {
                lastpos = blockSel.Position;
                lastfacing = blockSel.Face;
                
            }
            BlockEntityMicroBlock bmb = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityMicroBlock;

            if (bmb == null) { lastpos = null;  return 0; }
            CuboidWithMaterial cwm = new CuboidWithMaterial();
            if (bmb.VoxelCuboids == null || bmb.VoxelCuboids.Count == 0)
            {
                if (!(api is ICoreClientAPI)) { api.World.BlockAccessor.SetBlock(0, blockSel.Position);return 0; }
                else { return 0; }
            }

            int cutvoxels = 0;
            for (int xc = 0; xc < 16 / cutsize; xc++)
            {
                for (int yc = 0; yc < 16 / cutsize; yc++)
                {
                    if (lastfacing == BlockFacing.NORTH)
                    {
                        cutdepth = (int)(blockSel.HitPosition.Z * 16);
                        //bmb.SetVoxel(new Vec3i(xc * cutsize, yc * cutsize, cutcounter * cutsize), false, null, 0, cutsize);
                        bmb.SetVoxel(new Vec3i(xc * cutsize, yc * cutsize, cutdepth * cutsize), false, null, 0, cutsize);
                        cutvoxels++;
                    }
                    else if (lastfacing == BlockFacing.SOUTH)
                    {
                        cutdepth = (int)(blockSel.HitPosition.Z * 16) - cutsize;
                        //bmb.SetVoxel(new Vec3i(xc * cutsize, yc * cutsize, (16-cutsize)-cutcounter * cutsize), false, null, 0, cutsize);
                        bmb.SetVoxel(new Vec3i(xc * cutsize, yc * cutsize, cutdepth * cutsize), false, null, 0, cutsize);
                        cutvoxels++;
                    }
                    else if (lastfacing == BlockFacing.WEST)
                    {
                        cutdepth = (int)(blockSel.HitPosition.X * 16);
                        bmb.SetVoxel(new Vec3i(cutdepth * cutsize, yc * cutsize, xc * cutsize), false, null, 0, cutsize);
                        cutvoxels++;
                    }
                    else if (lastfacing == BlockFacing.EAST)
                    {
                        cutdepth = (int)(blockSel.HitPosition.X * 16) - cutsize;
                        bmb.SetVoxel(new Vec3i(cutdepth * cutsize, yc * cutsize, xc * cutsize), false, null, 0, cutsize);
                        cutvoxels++;
                    }
                    else if (lastfacing == BlockFacing.DOWN)
                    {
                        cutdepth = (int)(blockSel.HitPosition.Y * 16);
                        bmb.SetVoxel(new Vec3i(yc * cutsize, cutdepth * cutsize, xc * cutsize), false, null, 0, cutsize);
                        cutvoxels++;
                    }
                    else if (lastfacing == BlockFacing.UP)
                    {
                        cutdepth = (int)(blockSel.HitPosition.Y * 16) - cutsize;
                        bmb.SetVoxel(new Vec3i(yc * cutsize, cutdepth * cutsize, xc * cutsize), false, null, 0, cutsize);
                        cutvoxels++;
                    }
                }
            }
            bmb.MarkDirty(true);
            return cutvoxels;
            

        }
        /// <summary>
        /// This will try and find the targeted voxel and make a 16x16 plane of that voxel's material type
        /// (plane will be added in front of the targetd voxel)
        /// Returns number of voxels cut
        /// </summary>
        /// <param name="blockSel">Block Selection</param>
        
        public virtual int PlaneAdd(BlockSelection blockSel)
        {
            bool state = true;
            if (blockSel == null)
            {
                lastpos = null; return 0;
            }
            if (lastpos == null || lastpos != blockSel.Position || lastfacing != blockSel.Face)
            {
                lastpos = blockSel.Position;
                lastfacing = blockSel.Face;

            }
            BlockEntityMicroBlock bmb = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityMicroBlock;
            if (bmb == null) { lastpos = null; return 0; }
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
            int cutvoxels = 0;
            //loop thru 16 x 16 voxel plane (xc/yc will be swapped with other coordinates depending on the direction we are facing)
            //tell the microblock to add each voxel
            for (int xc = 0; xc < 16 / cutsize; xc++)
            {
                for (int yc = 0; yc < 16 / cutsize; yc++)
                {
                    if (lastfacing == BlockFacing.NORTH)
                    {
                        cutdepth = (int)(blockSel.HitPosition.Z * 16)-1;
                        if (cutdepth > 15||cutdepth<0) { return 0; }
                        bmb.SetVoxel(new Vec3i(xc * cutsize, yc * cutsize, cutdepth * cutsize), state, null, useindex, cutsize);
                        cutvoxels++;
                    }
                    else if (lastfacing == BlockFacing.SOUTH)
                    {
                        cutdepth = (int)(blockSel.HitPosition.Z * 16) ;
                        if (cutdepth > 15) { return 0; }
                        bmb.SetVoxel(new Vec3i(xc * cutsize, yc * cutsize, cutdepth * cutsize), state, null, useindex, cutsize);
                        cutvoxels++;
                    }
                    else if (lastfacing == BlockFacing.WEST)
                    {
                        cutdepth = (int)(blockSel.HitPosition.X * 16)-1;
                        if (cutdepth > 15 || cutdepth < 0) { return 0; }
                        bmb.SetVoxel(new Vec3i(cutdepth * cutsize, yc * cutsize, xc * cutsize), state, null, useindex, cutsize);
                        cutvoxels++;
                    }
                    else if (lastfacing == BlockFacing.EAST)
                    {
                        cutdepth = (int)(blockSel.HitPosition.X * 16) ;
                            if (cutdepth > 15) { return 0; };
                        bmb.SetVoxel(new Vec3i(cutdepth * cutsize, yc * cutsize, xc * cutsize), state, null, useindex, cutsize);
                        cutvoxels++;
                    }
                    else if (lastfacing == BlockFacing.DOWN)
                    {
                        cutdepth = (int)(blockSel.HitPosition.Y * 16)-1;
                        if (cutdepth > 15 || cutdepth < 0) { return 0; }
                        bmb.SetVoxel(new Vec3i(yc * cutsize, cutdepth * cutsize, xc * cutsize), state, null, useindex, cutsize);
                        cutvoxels++;
                    }
                    else if (lastfacing == BlockFacing.UP)
                    {
                        cutdepth = (int)(blockSel.HitPosition.Y * 16) ;
                        if (cutdepth > 15) { return 0; }
                        bmb.SetVoxel(new Vec3i(yc * cutsize, cutdepth * cutsize, xc * cutsize), state, null, useindex, cutsize);
                        cutvoxels++;
                    }
                }
            }
            bmb.MarkDirty(true);
            return cutvoxels;


        }

        
        /// <summary>
        /// Add or remove faces only matching a certain material
        /// Returns voxels cut
        /// </summary>
        /// <param name="blockSel">BlockSel from player</param>
        /// <param name="addmode">true - add, false - remove blocks</param>
        public virtual int ExtrudeAdd(BlockSelection blockSel,bool addmode)
        {
            if (blockSel == null)
            {
                lastpos = null; return 0;
            }
            if (lastpos == null || lastpos != blockSel.Position || lastfacing != blockSel.Face)
            {
                lastpos = blockSel.Position;
                lastfacing = blockSel.Face;

            }
            BlockEntityMicroBlock bmb = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityMicroBlock;
            if (bmb == null) { lastpos = null; return 0; }
            byte useindex = 0;
            CuboidWithMaterial cwm = new CuboidWithMaterial();
            //convert the hit point into voxel coordinates
            Vec3i s = new Vec3i((int)(blockSel.HitPosition.X * 16f), (int)(blockSel.HitPosition.Y * 16f), (int)(blockSel.HitPosition.Z * 16f));
            if (blockSel.Face == BlockFacing.SOUTH) { s.Z--; }
            if (blockSel.Face == BlockFacing.UP) { s.Y--; }
            if (blockSel.Face == BlockFacing.EAST) { s.X--; }

            List<CuboidWithMaterial> cuboidsInPlane = new List<CuboidWithMaterial>() ;
            Cuboidi checkplane = new Cuboidi();
            Vec3i startco = new Vec3i(s.X,s.Y,s.Z); //start coordinates of our build plane
            Vec3i endco = new Vec3i(s.X, s.Y, s.Z); //end coordinates of our build plane
            Vec3i writeoffset = new Vec3i(0,0,0);

            
            //adjust the plane based on facing
            if (blockSel.Face == BlockFacing.NORTH||blockSel.Face==BlockFacing.SOUTH)
            {
                startco.X = 0;startco.Y = 0;
                endco.X = 16;endco.Y = 16;
                endco.Z += 1;
                if (addmode)
                {
                    if (blockSel.Face == BlockFacing.SOUTH) { writeoffset.Z = 1; }
                    else { writeoffset.Z = -1; }
                }
            }
            else if (blockSel.Face == BlockFacing.EAST || blockSel.Face == BlockFacing.WEST)
            {
                startco.Z = 0; startco.Y = 0;
                endco.Z = 16;endco.Y = 16;
                endco.X += 1;
                if (addmode)
                {
                    if (blockSel.Face == BlockFacing.EAST) { writeoffset.X = 1; }
                    else { writeoffset.X = -1; }
                }
            }
            else if (blockSel.Face == BlockFacing.DOWN || blockSel.Face == BlockFacing.UP)
            {
                startco.X = 0;startco.Z = 0;
                endco.X = 16;endco.Z = 16;
                endco.Y += 1;
                if (addmode)
                {
                    if (blockSel.Face == BlockFacing.UP) { writeoffset.Y = 1; }
                    else { writeoffset.Y = -1; }
                }
            }
            checkplane.Set(startco, endco);

            //Loop thru the cuboids and find the material we are looking at
            foreach (uint voxint in bmb.VoxelCuboids)
            {
                //this static method converts the uint into a CubioidWithMaterial
                BlockEntityMicroBlock.FromUint(voxint, cwm);
                //if we're looking at this voxel we want to grab this material to use
                if (cwm.Contains(s.X, s.Y, s.Z)) { useindex = cwm.Material;break;  }    
            }
            
            //now we go thru the cuboids again - we need to grab cuboids that intersect with our plane and that have the appropriate material
            foreach (uint voxint in bmb.VoxelCuboids)
            {
                
                CuboidWithMaterial checkcuboid = new CuboidWithMaterial();
                BlockEntityMicroBlock.FromUint(voxint, checkcuboid);//convert raw data into a cuboid we can read
                if (checkcuboid.Material != useindex) { continue; }//reject cuboids that don't share our material
                if (checkcuboid.Intersects(checkplane)) { cuboidsInPlane.Add(checkcuboid); }//if it's in the check plane then add it
            }
            if (cuboidsInPlane.Count == 0) { return 0; } //I don't think this is necessary, but maybe if there's a race condition?
            
            //loop thru 16 x 16 voxel plane (xc/yc will be swapped with other coordinates depending on the direction we are facing)
            //tell the microblock to add each voxel
            //adjust the start and end coordinates to work with the for loop:
            endco.X++;
            endco.Y++;
            endco.Z++;
            int cutvoxels = 0;
            //Loop thru all the dimensions of our plane (one dimension should be 0 size)
            for (int xc = startco.X; xc < endco.X; xc++)
            {
                for (int yc= startco.Y; yc < endco.Y; yc++)
                {
                    for (int zc = startco.Z; zc < endco.Z; zc++)
                    {
                        BlockPos v = new BlockPos(xc, yc, zc);
                        if (v.X + writeoffset.X > 15 || v.X + writeoffset.X < 0) { continue; }
                        if (v.Y + writeoffset.Y > 15 || v.Y + writeoffset.Y < 0) { continue; }
                        if (v.Z + writeoffset.Z > 15 || v.Z + writeoffset.Z < 0) { continue; }
                        foreach (CuboidWithMaterial cube in cuboidsInPlane)
                        {
                            if (!cube.Contains(v)) { continue; }
                            
                            bmb.SetVoxel(new Vec3i(v.X+writeoffset.X,v.Y+writeoffset.Y,v.Z+writeoffset.Z), addmode, null, useindex, 1);
                            cutvoxels++;
                            break;
                        }
                    }
                }
            }

            bmb.MarkDirty(true);
            return cutvoxels;
        }

    }
}
