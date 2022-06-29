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

namespace RustAndRails.src
{
    class BlockSignalSwitch:Block,IRailwaySignalReceiver
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            //Note this means the player switching this will send a very strong signal
            ReceiveRailwaySignal(world, blockSel.Position,16,"SWITCH");
            return true;
        }
        protected virtual void ActivateSwitch(IWorldAccessor world, BlockPos pos,int strength,string signal)
        {
            BlockPos checkpos = pos.Copy().Offset(BlockFacing.FromCode(LastCodePart()));
            Block checkblock = world.BlockAccessor.GetBlock(checkpos);
            if (checkblock is IRailwaySignalReceiver)
            {
                IRailwaySignalReceiver sig=checkblock  as IRailwaySignalReceiver;
                sig.ReceiveRailwaySignal(world, checkpos,strength, signal);
            }
            else if (checkblock.Attributes != null)
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
            Block me = world.BlockAccessor.GetBlock(pos);
            if (me.Attributes != null)
            {
                string switchme = me.Attributes["replace"].AsString(null);
                if (switchme != null)
                {
                    Block newswitch = world.GetBlock(new AssetLocation(switchme));
                    if (switchme != null)
                    {
                        world.BlockAccessor.SetBlock(newswitch.Id, pos);
                    }
                }
            }
        }

        public void ReceiveRailwaySignal(IWorldAccessor world, BlockPos pos, int strength,string signal)
        {
            strength--;
            if (strength <= 0) { return; }
            ActivateSwitch(world, pos,strength,signal);
        }
    }
}
