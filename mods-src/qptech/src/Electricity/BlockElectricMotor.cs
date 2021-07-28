﻿using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

namespace qptech.src
{
    class BlockElectricMotor : ElectricalBlock
    {
        int facing;

        public override void OnLoaded(ICoreAPI api)
        {
            int iOfA = BlockFacing.FromCode(Variant["side"]).Index;
            int iOfB = BlockFacing.FromCode(Variant["side"]).Opposite.Index;

            foreach (BlockFacing face in BlockFacing.HORIZONTALS)
            {
                if (face.Index != iOfA && face.Index != iOfB) facing |= 1 << face.Index;
            }

            base.OnLoaded(api);
        }

        public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
        {

        }

        public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
        {
            return (facing >> face.Index & 1) > 0;
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, blockPos, byItemStack);
            
            foreach (BlockFacing face in BlockFacing.HORIZONTALS)
            {
                BlockPos pos = blockPos.AddCopy(face);
                IMechanicalPowerBlock block = world.BlockAccessor.GetBlock(pos) as IMechanicalPowerBlock;
                if (block != null)
                {
                    if (block.HasMechPowerConnectorAt(world, pos, face.Opposite))
                    {
                        block.DidConnectAt(world, pos, face.Opposite);
                    }
                }
            }
        }
    }
}
