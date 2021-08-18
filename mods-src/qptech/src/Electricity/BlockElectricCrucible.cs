using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace qptech.src
{
    class BlockElectricCrucible : ElectricalBlock
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            
            BEElectricCrucible bee = (BEElectricCrucible)api.World.BlockAccessor.GetBlockEntity(blockSel.Position);
            if (bee != null) { bee.OpenStatusGUI(); return true; }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }
}
