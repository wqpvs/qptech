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
    class BlockDoorPart:Block
    {
        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            ISlaveBlock me = world.BlockAccessor.GetBlockEntity(pos) as ISlaveBlock;
            if (me!=null && me.Initialized && me.Master == null)
            {
                world.BlockAccessor.BreakBlock(pos, null);
            }
            base.OnNeighbourBlockChange(world, pos, neibpos);
        }
    }
}
