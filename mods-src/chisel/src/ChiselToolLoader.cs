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

namespace chisel.src
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
            sapi.RegisterCommand("qpchisel-handplaner-toolusage", "Set how fast hand planer gets damaged when used. Default is 0.125. ", "", CmdSetHandPlanerMultiplier,Privilege.controlserver);
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
            api.RegisterItemClass("ItemWedge", typeof(ItemWedge));
        }

        private void CmdSetHandPlanerMultiplier(IPlayer player, int groupId, CmdArgs args)
        {
            if (sapi == null) { return; }
            if (serverconfig == null) { sapi.BroadcastMessageToAllGroups("QP Chisel Server Config doesn't exist.", EnumChatType.Notification); return; }
            if (args == null || args.Length == 0)
            {
                sapi.SendMessage(player, groupId, "Hand Planer tool usage rate is " + serverconfig.handPlanerBaseDurabilityMultiplier + "(Default is 0.125)", EnumChatType.CommandSuccess);
                return;
            }
            float newvalue = args[0].ToFloat(0.125f);
            serverconfig.handPlanerBaseDurabilityMultiplier = newvalue;
            sapi.StoreModConfig<ChiselToolServerData>(serverconfig, serverconfigfile);
            sapi.SendMessage(player, groupId, "Hand Planer tool usage rate SET to " + serverconfig.handPlanerBaseDurabilityMultiplier + "(Default is 0.125)", EnumChatType.CommandSuccess);
        }

        [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
        public class ChiselToolServerData
        {
            public int handPlanerBaseDurabilityUse = 1;
            public float handPlanerBaseDurabilityMultiplier = 0.125f;
            
        }
    }
}
