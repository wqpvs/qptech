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
    class BEReportsClicks:BlockEntity,ISlaveBlock
    {
        IMasterBlock master;
        bool initialized = false;
        public IMasterBlock Master => master;
        public bool Initialized => initialized&&Master!=null;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
        }
        public void Initialize(IMasterBlock master)
        {
            this.master = master;
            initialized = true;
        }
        public override void OnBlockRemoved()
        {
            if (initialized)
            {
                master.OnMemberRemoved();
            }
            base.OnBlockRemoved();
        }

    }
}
