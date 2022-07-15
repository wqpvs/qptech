using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.API.Util;


namespace modernblocks.src
{
    class BlockConnectedTexture : Block
    {
        //to do this should actually trigger an update in 3x3x3 area around the placed block on other ct blocks

        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            base.OnNeighbourBlockChange(world, pos, neibpos);
            BEConnectedTextures bem = world.BlockAccessor.GetBlockEntity(pos) as BEConnectedTextures;
            if (bem != null) { 
                bem.FindNeighbours();
            }
        }
    }
}