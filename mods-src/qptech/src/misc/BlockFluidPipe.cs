﻿using System.Linq;
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
    }
}