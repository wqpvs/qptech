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
    class BlockDetectorRail:Block
    {
        public void CartDetected(ICoreAPI api, MinecartEntity cart,BlockPos blockpos)
        {
            if (api==null||cart==null||Attributes == null) { return; }
            string replace = Attributes["replace"].AsString(null);
            if (replace == null) { return; }
            Block replaceblock = api.World.GetBlock(new AssetLocation(replace));
            if (replaceblock == null) { return; }
            
            //search for and activate switches - this will eventually be a generic switching behaviour
            foreach(BlockFacing bf in BlockFacing.ALLFACES)
            {
                BlockPos checkpos = blockpos.Copy().Offset(bf);
                BlockSignalSwitch signalswitch = api.World.BlockAccessor.GetBlock(checkpos) as BlockSignalSwitch;
                if (signalswitch != null)
                {
                    signalswitch.ActivateSwitch(api.World, checkpos);
                }
                else
                {
                    Block checkblock = api.World.BlockAccessor.GetBlock(checkpos);
                    if (checkblock.Attributes != null)
                    {
                        string switchblock = checkblock.Attributes["railswitch"].AsString(null);
                        if (switchblock != null)
                        {
                            Block newrail = api.World.GetBlock(new AssetLocation(switchblock));
                            if (newrail != null)
                            {
                                api.World.BlockAccessor.SetBlock(newrail.BlockId, checkpos);

                            }
                        }
                    }
                }
            }
            api.World.BlockAccessor.SetBlock(replaceblock.BlockId, blockpos);
        }
    }
}
