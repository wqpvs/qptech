using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.API.Config;

namespace qptech.src.itemtransport
{
    class StorageDepot:ModSystem
    {
        ICoreClientAPI capi;
        ICoreServerAPI sapi;
        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            if (api is ICoreClientAPI) { capi = api as ICoreClientAPI; }
            else { sapi = api as ICoreServerAPI; }
            
        }


    }
}
