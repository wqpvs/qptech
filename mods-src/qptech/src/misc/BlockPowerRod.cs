using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using System.Collections.Generic;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Client;

namespace qptech.src
{
    class BlockPowerRod : BlockGroundAndSideAttachable
    {
        Dictionary<string, Cuboidi> attachmentAreas;
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            var areas = Attributes?["attachmentAreas"].AsObject<Dictionary<string, RotatableCube>>(null);
       
            if (areas != null)
            {
                attachmentAreas = new Dictionary<string, Cuboidi>();
                foreach (var val in areas)
                {
                    val.Value.Origin.Set(8, 8, 8);
                    attachmentAreas[val.Key] = val.Value.RotatedCopy().ConvertToCuboidi();
                }
            }
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            if (byPlayer.Entity.Controls.Sneak)
            {
                failureCode = "__ignore__";
                return false;
            }

            if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
            {
                return false;
            }

            // Prefer selected block face
            if (blockSel.Face.IsHorizontal || blockSel.Face == BlockFacing.UP)
            {
                if (TryAttachTo(world, blockSel.Position, blockSel.Face)) return true;
            }

            if (blockSel.Face.IsHorizontal || blockSel.Face == BlockFacing.DOWN)
            {
                if (TryAttachTo(world, blockSel.Position, blockSel.Face)) return true;
            }

            // Otherwise attach to any possible face

            BlockFacing[] faces = BlockFacing.ALLFACES;
            for (int i = 0; i < faces.Length; i++)
            {
                if (faces[i] == BlockFacing.DOWN) continue;

                if (TryAttachTo(world, blockSel.Position, faces[i])) return true;
            }

            failureCode = "requireattachable";

            return false;
        }

        bool TryAttachTo(IWorldAccessor world, BlockPos blockpos, BlockFacing onBlockFace)
        {
            BlockFacing onFace = onBlockFace;

            BlockPos attachingBlockPos = blockpos.AddCopy(onBlockFace.Opposite);
            Block block = world.BlockAccessor.GetBlock(attachingBlockPos);

            Cuboidi attachmentArea = null;
            attachmentAreas?.TryGetValue(onBlockFace.Opposite.Code, out attachmentArea);

            if (block.CanAttachBlockAt(world.BlockAccessor, this, attachingBlockPos, onFace, attachmentArea))
            {
                int blockId = world.BlockAccessor.GetBlock(CodeWithVariant("orientation", onBlockFace.Code)).BlockId;
                world.BlockAccessor.SetBlock(blockId, blockpos);
                return true;
            }

            return false;
        }

        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            if (HasBehavior<BlockBehaviorUnstableFalling>())
            {
                base.OnNeighbourBlockChange(world, pos, neibpos);
                return;
            }

            if (!CanStay(world.BlockAccessor, pos))
            {
                world.BlockAccessor.BreakBlock(pos, null);
            }

            BEElectric bee = world.BlockAccessor.GetBlockEntity(pos) as BEElectric;
            if (bee != null)
            {
                bee.FindConnections();
            }
            base.OnNeighbourBlockChange(world, pos, neibpos);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
  
            var bee = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEElectric;

            if (bee != null && byPlayer.Entity.RightHandItemSlot.Itemstack == null)
            {
                return base.OnBlockInteractStart(world, byPlayer, blockSel);
            }


            if (byPlayer.Entity.RightHandItemSlot.Itemstack.Item is ItemWire)
            {
                return bee.OnWireClick(world, byPlayer, blockSel);
           
            }

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }


        bool CanStay(IBlockAccessor blockAccessor, BlockPos pos)
        {
            BlockFacing facing = BlockFacing.FromCode(Variant["orientation"]);
            BlockPos attachingBlockPos = pos.AddCopy(facing.Opposite);

            Block block = blockAccessor.GetBlock(attachingBlockPos);

            Cuboidi attachmentArea = null;
            attachmentAreas?.TryGetValue(facing.Opposite.Code, out attachmentArea);

            return block.CanAttachBlockAt(blockAccessor, this, attachingBlockPos, facing, attachmentArea);
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            List<ItemStack> wireStacklist = new List<ItemStack>();
            foreach (Item item in api.World.Items)
            {
                if (item is ItemWire)
                {
                    wireStacklist?.Add(new ItemStack(item));
                }
            }
            return new WorldInteraction[]
                    {
                        new WorldInteraction()
                        {
                        ActionLangCode = "machines:blockhelp-cable",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = wireStacklist.ToArray(),
                        GetMatchingStacks = (wi, bs, es) => {
                        BEElectric bea = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BEElectric;
                        return bea?.AcceptsDirectPower == false ? null : wi.Itemstacks;
                        }
                    }
            };
        }

    }
}
