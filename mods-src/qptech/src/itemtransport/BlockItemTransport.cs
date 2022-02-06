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
            var itemtransporter = world.BlockAccessor.GetBlockEntity(blockSel.Position) as IItemTransporter;
            if (itemtransporter == null) { return base.OnBlockInteractStart(world, byPlayer, blockSel); }
            bool tryconveyor = itemtransporter.OnBlockInteractStart(world, byPlayer, blockSel);
            if (tryconveyor) { return false; }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);

            
        }
    }
}
