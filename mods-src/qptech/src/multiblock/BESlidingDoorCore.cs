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
    class BESlidingDoorCore:BlockEntity,IMasterBlock
    {
        List<ISlaveBlock> slaveblocks;
        public List<ISlaveBlock> SlaveBlocks { get { if (slaveblocks == null) { slaveblocks = new List<ISlaveBlock>(); } return slaveblocks; } set { slaveblocks = value; } }

    }
}
