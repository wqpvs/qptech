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
    /// <summary>
    /// This is an invisible placeholder block for oversized blocks
    /// </summary>
    class BEDummyBlock:BlockEntity
    {
        public IDummyParent parentblock;
        bool informed = false;
        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            informed = true;
            if (parentblock != null&&!informed) { parentblock.OnDummyBroken(); }
        }
        public void ParentBroken()
        {
            informed = true;
            Api.World.BlockAccessor.SetBlock(0, Pos);
        }
    }

    public interface IDummyParent
    {
        void OnDummyBroken();
    }
}
