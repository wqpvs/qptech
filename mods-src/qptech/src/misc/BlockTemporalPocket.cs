using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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


namespace qptech.src
{
    class BlockTemporalPocket:BlockGenericTypedContainer
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BETemporalPocket mypocket = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BETemporalPocket;
            if (mypocket!=null && mypocket.Busy) { return false; }
            //if (simpleinventory.openinventories != null && simpleinventory.openinventories.Contains(byPlayer.PlayerUID)) { return false; }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

    }
    
}
