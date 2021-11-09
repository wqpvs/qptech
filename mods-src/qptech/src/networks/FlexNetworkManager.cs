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
        public static List<IFlexNetwork> NetworkList {
            get
            {
                if (networklist == null) { LoadNetworks(); }
                return networklist;
            }
        }
        static List<IFlexNetwork> networklist;
        public static FlexNetworkManager manager;
        
        public override void Start(ICoreAPI api)
        {
            if (!(api is ICoreServerAPI)) { return; }
            this.api = api;
            ICoreServerAPI capi = api as ICoreServerAPI;
            
            api.Event.RegisterGameTickListener(OnTick, 75);
            if (manager == null) { manager = this; }

        }
        public static void LeaveNetwork(Guid networkid,IFlexNetworkMember member)
        {
            IFlexNetwork fn = GetNetworkWithID(networkid);
            if (fn == null) { return; }
            fn.GetMembers().Remove(member);
        }
        public static IFlexNetwork GetNetworkWithID(Guid networkid)
        {
            if (networkid == Guid.Empty) { return null; }
            if (NetworkList == null || NetworkList.Count == 0) { return null; }

            IFlexNetwork[] fn=NetworkList.ToArray();
            
            foreach (IFlexNetwork f in fn)
            {
                if (f == null) { continue; }
                if (f.NetworkID == networkid) { return f; }
            }
            
            return null;
        }
        public static bool JoinNetworkWithID(Guid networkid,IFlexNetworkMember newmember)
        {
            
            if (networkid == Guid.Empty) { return false; }
            IFlexNetwork n = GetNetworkWithID(networkid);
            
            if (n == null) { RecreateNetwork(networkid,newmember.ProductID); n = GetNetworkWithID(networkid); }
            
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
        public static void RecreateNetwork(Guid existingid, string networktype)
        {
            
            if (existingid == Guid.Empty) { return; }
            if (GetNetworkWithID(existingid) != null) { return; }
            if (networktype == "power")
            {
                PowerNetwork pn = new PowerNetwork(existingid);
                NetworkList.Add(pn);
            }
            
        }
        public static bool DeleteNetwork(Guid g)
        {
            IFlexNetwork n = GetNetworkWithID(g);
            if (n == null) { return false; }
            
            NetworkList.Remove(n);
            n.RemoveNetwork();
            
            return true;
        }
        public static void LoadNetworks()
        {
            networklist = new List<IFlexNetwork>();
        }
        public static void MergeNetworks(Guid id1,Guid id2)
        {
            if (id1 == id2 || id1 == Guid.Empty || id2 == Guid.Empty) { return; }
            IFlexNetwork n1 = GetNetworkWithID(id1);
            if (n1 == null) { return; }
            IFlexNetwork n2 = GetNetworkWithID(id2);
            if (n2 == null) { return; }
            if (n1.ProductID != n2.ProductID) { return; }

            foreach (IFlexNetworkMember n2member in n2.GetMembers())
            {
                n2member.NetworkJoin(id1);

            }
            n1.RemoveNetwork();

        }
        public void OnTick(float dt)
        {
            if (!(api is ICoreServerAPI)) { return; }

            List<IFlexNetwork> prune = new List<IFlexNetwork>();

            
            foreach (IFlexNetwork n in NetworkList)
            {
                if (n.GetMembers().Count == 0)
                {
                    prune.Add(n);
                }
                else if (n != null) { 
                    n.OnTick(dt);
                }
            }
            
            foreach (IFlexNetwork pn in prune)
            {
                NetworkList.Remove(pn);
            }
            
        }
    }
}
