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
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            
            
            BEReportsClicks brc = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BEReportsClicks;
            if (brc != null )
            {
                brc.Interact(byPlayer);
                return true;
            }

            IMasterBlock bee = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as IMasterBlock;
            if (bee != null) { bee.Interact(byPlayer); return true; }
            
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }
}
