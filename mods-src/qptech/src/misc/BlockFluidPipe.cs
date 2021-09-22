using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using System.Collections.Generic;

namespace qptech.src
{
    class BlockFluidPipe:Block
    {
 
        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            base.OnNeighbourBlockChange(world, pos, neibpos);
            BEFluidPipe myentity= world.BlockAccessor.GetBlockEntity(pos) as BEFluidPipe;
            if (myentity != null) { myentity.OnNeighborChange(); }
        }
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BEFluidPipe myentity = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEFluidPipe;
            if (myentity != null) {
                if (myentity.OnInteract(world, byPlayer, blockSel)) { return true; } ;
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }


        public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos) {
            return base.GetCollisionBoxes(blockAccessor, pos); 
        }

        public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            return base.GetSelectionBoxes(blockAccessor, pos);
        }

        public override void OnBeingLookedAt(IPlayer byPlayer, BlockSelection blockSel, bool firstTick)
        {
            BEFluidPipe myentity = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BEFluidPipe;
            if (myentity != null)
            {
                myentity.OnBeingLookedAt(byPlayer, blockSel, firstTick);
            }
            base.OnBeingLookedAt(byPlayer, blockSel, firstTick);
        }
    }
}
