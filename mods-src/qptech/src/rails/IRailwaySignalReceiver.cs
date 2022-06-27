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
    interface IRailwaySignalReceiver
    {
        /// <summary>
        /// Railway signal system - blocks and send signals to each toher
        /// </summary>
        /// <param name="world"></param>
        /// <param name="pos">Postion of this block</param>
        /// <param name="strength">Strength of the signal - most blocks would reduce by 1</param>
        /// <param name="signal">Consider a channel, or can be used for whatever info you want to check</param>
        void ReceiveRailwaySignal(IWorldAccessor world, BlockPos pos,int strength,string signal);
    }
}
