using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.API.Datastructures;
using ProtoBuf;

namespace chiseltools
{
    class ChiselToolLoader : ModSystem
    {
        public static ChiselToolLoader loader;
        ICoreClientAPI capi;
        ICoreServerAPI sapi;
        string serverconfigfile = "qpchiseltoolserversettings.json";
        public static ChiselToolServerData serverconfig;
        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            if (api is ICoreClientAPI)
            {
                capi = api as ICoreClientAPI;
            }
            else if (api is ICoreServerAPI)
            {
                sapi = api as ICoreServerAPI;
                ServerPreStart();
            }
            loader = this;
        }

        void ServerPreStart()
        {
            try
            {
                serverconfig = sapi.LoadModConfig<ChiselToolServerData>(serverconfigfile);
            }
            catch
            {


            }
            if (serverconfig == null)
            {
                serverconfig = new ChiselToolServerData();
                sapi.StoreModConfig<ChiselToolServerData>(serverconfig, serverconfigfile);
            }
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterItemClass("ItemHandPlaner", typeof(ItemHandPlaner));
        }

        [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
        public class ChiselToolServerData
        {
            public int handPlanerBaseDurabilityUse = 1;
            public float handPlanerBaseDurabilityMultiplier = 0.125f;
            
        }
    }
}
