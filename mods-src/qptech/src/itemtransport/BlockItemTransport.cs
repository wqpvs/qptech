using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VintagestoryAPI;
using VintagestoryAPI.Math;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Client;
using Vintagestory.API.Server;


namespace qptech.src.itemtransport
{
    class BlockItemTransport:Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            var conveyor = world.BlockAccessor.GetBlockEntity(blockSel.Position) as Conveyor;
            if (conveyor == null) { return base.OnBlockInteractStart(world, byPlayer, blockSel); }
            bool tryconveyor = conveyor.OnPlayerInteract(byPlayer);
            if (tryconveyor) { return false; }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);

            
        }
    }
}
