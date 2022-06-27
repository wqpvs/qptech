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
    class BlockDetectorRail:BlockRail,IRailwaySignalReceiver
    {
        protected void CartDetected(IWorldAccessor world, BlockPos blockpos,int strength,string signal)
        {
            
            if (world==null||Attributes == null) { return; }
            if (!signal.Contains("cart")) { return; }
            string replace = Attributes["replace"].AsString(null);
            if (replace == null) { return; }
            Block replaceblock = world.GetBlock(new AssetLocation(replace));
            if (replaceblock == null) { return; }
            
            //search for and activate switches - this will eventually be a generic switching behaviour
            foreach(BlockFacing bf in BlockFacing.ALLFACES)
            {
                BlockPos checkpos = blockpos.Copy().Offset(bf);
                IRailwaySignalReceiver signalswitch = world.BlockAccessor.GetBlock(checkpos) as IRailwaySignalReceiver;
                if (signalswitch != null)
                {
                    signalswitch.ReceiveRailwaySignal(world, checkpos,strength,signal);
                }
                else
                {
                    Block checkblock = world.BlockAccessor.GetBlock(checkpos);
                    if (checkblock.Attributes != null)
                    {
                        string switchblock = checkblock.Attributes["railswitch"].AsString(null);
                        if (switchblock != null)
                        {
                            Block newrail = world.GetBlock(new AssetLocation(switchblock));
                            if (newrail != null)
                            {
                                world.BlockAccessor.SetBlock(newrail.BlockId, checkpos);

                            }
                        }
                    }
                }
            }
            world.BlockAccessor.SetBlock(replaceblock.BlockId, blockpos);
        }

        public virtual void ReceiveRailwaySignal(IWorldAccessor world, BlockPos pos,int strength, string signal)
        {
            strength--;
            if (strength <= 0) { return; }
            CartDetected(world,pos,strength,signal);
        }
    }
}
