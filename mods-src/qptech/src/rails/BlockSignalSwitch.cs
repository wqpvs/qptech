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

namespace qptech.src.rails
{
    class BlockSignalSwitch:Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockPos checkpos = blockSel.Position.Copy().Offset(BlockFacing.FromCode(LastCodePart()));
            Block checkblock = world.BlockAccessor.GetBlock(checkpos);
            if (checkblock.Attributes == null) { return base.OnBlockInteractStart(world, byPlayer, blockSel); }
            string switchblock = checkblock.Attributes["railswitch"].AsString(null);
            if (switchblock != null)
            {
                Block newrail = world.GetBlock(new AssetLocation(switchblock));
                if (newrail != null)
                {
                    world.BlockAccessor.SetBlock(newrail.BlockId, checkpos);
                    return true;
                }
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }
}
