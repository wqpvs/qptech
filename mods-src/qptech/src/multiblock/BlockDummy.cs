using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Server;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;
namespace qptech.src.multiblock
{
    class BlockDummy:Block
    {
        public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
        {
            BEDummyBlock dummy = api.World.BlockAccessor.GetBlockEntity(pos) as BEDummyBlock;
            if (dummy != null) { return dummy.displaytext; }
            return base.GetPlacedBlockName(world, pos);
        }

    }
}
