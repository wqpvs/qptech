using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
namespace qptech.src.networks
{
    class FlexNetworkManager:ModSystem
    {
        ICoreAPI api;
        public static List<FlexNetwork> NetworkList {
            get
            {
                if (networklist == null) { LoadNetworks(); }
                return networklist;
            }
        }
        static List<FlexNetwork> networklist;
        public static FlexNetworkManager manager;
        public override void Start(ICoreAPI api)
        {
            if (!(api is ICoreServerAPI)) { return; }
            this.api = api;
            ICoreServerAPI capi = api as ICoreServerAPI;
            
            api.Event.RegisterGameTickListener(OnTick, 75);
            manager = this;
            
        }
        public static FlexNetwork GetNetworkWithID(Guid networkid)
        {
            if (networkid == Guid.Empty) { return null; }
            if (NetworkList == null || NetworkList.Count == 0) { return null; }
            FlexNetwork fn = null;
            foreach (FlexNetwork f in NetworkList)
            {
                if (f == null) { continue; }
                if (f.NetworkID == networkid) { return f; }
            }

            return null;
        }
        public static bool JoinNetworkWithID(Guid networkid,FlexNetworkMember newmember)
        {
            
            FlexNetwork n = GetNetworkWithID(networkid);
            if (n == null) { return false; }
            bool result = n.JoinNetwork(newmember);
            
            return result;
        }
        public static Guid RequestNewNetwork(string networktype)
        {
            Guid g = Guid.NewGuid();
            if (networktype == "power")
            {
                PowerNetwork pn = new PowerNetwork(g);
                NetworkList.Add(pn);
            }
            return g;
        }
        public static bool DeleteNetwork(Guid g)
        {
            FlexNetwork n = GetNetworkWithID(g);
            if (n == null) { return false; }
            NetworkList.Remove(n);
            n.RemoveNetwork();

            return true;
        }
        public static void LoadNetworks()
        {
            networklist = new List<FlexNetwork>();
        }
        public static void MergeNetworks(Guid id1,Guid id2)
        {
            if (id1 == id2 || id1 == Guid.Empty || id2 == Guid.Empty) { return; }
            FlexNetwork n1 = GetNetworkWithID(id1);
            if (n1 == null) { return; }
            FlexNetwork n2 = GetNetworkWithID(id2);
            if (n2 == null) { return; }
            if (n1.ProductID != n2.ProductID) { return; }

            foreach (FlexNetworkMember n2member in n2.GetMembers())
            {
                n2member.NetworkJoin(id1);

            }
            n1.RemoveNetwork();

        }
        public void OnTick(float dt)
        {
            if (!(api is ICoreServerAPI)) { return; }
            if (manager != this) { 
                if (1 == 1)
                {

                }
            }
            List<FlexNetwork> prune = new List<FlexNetwork>();
            foreach (FlexNetwork n in NetworkList)
            {
                if (n.GetMembers().Count == 0)
                {
                    prune.Add(n);
                }
                else if (n != null) { 
                    n.OnTick(dt);
                }
            }
            foreach (FlexNetwork pn in prune)
            {
                NetworkList.Remove(pn);
            }
        }
    }
}
