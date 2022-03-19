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

namespace chisel.src
{
    class ItemWedge:ItemHandPlaner
    {
        public override int ModifyChiseledBlock(BlockSelection blockSel, string mode)
        {
            BlockEntityMicroBlock bmb = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityMicroBlock;
            if (bmb == null) { lastpos = null; return 0; }
            
            //convert the hit point into voxel coordinates
            //Vec3i s = new Vec3i((int)(blockSel.HitPosition.X * 16f), (int)(blockSel.HitPosition.Y * 16f), (int)(blockSel.HitPosition.Z * 16f));
            //if (blockSel.Face == BlockFacing.SOUTH) { s.Z--; }
            //if (blockSel.Face == BlockFacing.UP) { s.Y--; }
            //if (blockSel.Face == BlockFacing.EAST) { s.X--; }
            Vec3i writeoffset = new Vec3i(0, 0, 0);
            
            //adjust the plane based on facing
            if (blockSel.Face == BlockFacing.SOUTH) { writeoffset.Z = -1; }
            else if (blockSel.Face==BlockFacing.NORTH) { writeoffset.Z = 1; }
            else if (blockSel.Face == BlockFacing.EAST) { writeoffset.X = -1; }
            else if (blockSel.Face==BlockFacing.WEST){ writeoffset.X = 1; }
            else if (blockSel.Face == BlockFacing.UP) { writeoffset.Y = -1; }
            else if (blockSel.Face==BlockFacing.DOWN){ writeoffset.Y = 1; }

            List<uint> cuboids = new List<uint>();
            //Loop thru the cuboids and find the material we are looking at
            foreach (uint voxint in bmb.VoxelCuboids)
            {
                CuboidWithMaterial cwm = new CuboidWithMaterial();
                //this static method converts the uint into a CubioidWithMaterial
                BlockEntityMicroBlock.FromUint(voxint, cwm);
                //Shift coordinates of every cuboid - possible issue - voxels stacking up at outside of object
                cwm.X1 += writeoffset.X;cwm.X1 = GameMath.Clamp(cwm.X1, 0, 16);
                cwm.X2 += writeoffset.X;cwm.X2 = GameMath.Clamp(cwm.X2, 0, 16);
                if (cwm.X2 <= cwm.X1) { continue; }
                cwm.Y1 += writeoffset.Y; cwm.Y1 = GameMath.Clamp(cwm.Y1, 0, 16);
                cwm.Y2 += writeoffset.Y; cwm.Y2 = GameMath.Clamp(cwm.Y2, 0, 16);
                if (cwm.Y2 <= cwm.Y1) { continue; }
                cwm.Z1 += writeoffset.Z; cwm.Z1 = GameMath.Clamp(cwm.Z1, 0, 16);
                cwm.Z2 += writeoffset.Z; cwm.Z2 = GameMath.Clamp(cwm.Z2, 0, 16);
                if (cwm.Z2 <= cwm.Z1) { continue; }
                uint voxelint = BlockEntityMicroBlock.ToUint(cwm.X1, cwm.Y1, cwm.Z1, cwm.X2, cwm.Y2, cwm.Z2, cwm.Material);
                cuboids.Add(voxelint);
            }
            if (cuboids == null || cuboids.Count == 0) { return 0; }
            bmb.VoxelCuboids = cuboids;
            bmb.MarkDirty(true);
            return 16*16; //todo calculate actual voxels based on the bounds of the cuboids
        }
    }
}
