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
    interface IFunctionalMultiblockPart
    {
        IFunctionalMultiblockMaster Master { get; set; }
        void OnPartTick(float f);
    }

    interface IFunctionalMultiblockMaster
    {
        List<IFunctionalMultiblockPart> Parts { get; }
        MultiblockStructure MBStructure { get; }
        BlockPos MBOffset { get; }
        string StructureName { get; }
    }
}
