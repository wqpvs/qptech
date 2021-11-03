using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
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
            this.api = api;
            
            manager = this;
        }
        public static FlexNetwork GetNetworkWithID(Guid networkid)
        {
            var findnet = NetworkList.Where(x => x.NetworkID == networkid).First() as FlexNetwork;

            return findnet;
        }

        public static Guid RequestNewNetwork(string networktype)
        {
            Guid g = new Guid();
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
    }
}
