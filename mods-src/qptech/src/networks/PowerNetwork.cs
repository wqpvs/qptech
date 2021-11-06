using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace qptech.src.networks
{
    class PowerNetwork : FlexNetwork
    {
        Guid networkid;
        string productid="power";
        int powergen;
        int poweruse;
        
        public int PowerGeneration => powergen;
        public int PowerUse => poweruse;
        List<FlexNetworkMember> members;
        public Guid NetworkID => networkid;
        public string ProductID => productid;
        PowerNetworkInfo networkstatus;
        public PowerNetworkInfo NetworkStatus=>networkstatus;
        public PowerNetwork(Guid g)
        {
            networkid = g;
            networkstatus = new PowerNetworkInfo();
            //SET NETWORK ID - need a master network manager
        }
        public List<FlexNetworkMember> GetMembers()
        {
            if (members == null) { members = new List<FlexNetworkMember>(); }
            return members;
        }
        public bool JoinNetwork(FlexNetworkMember newmember)
        {
            if (newmember.ProductID != ProductID) { return false; }
            if (newmember as PowerNetworkMember == null) { return false; }
            if (!GetMembers().Contains(newmember)) { GetMembers().Add(newmember);return true; }
            return false;
        }
        public bool MergeWith(Guid newnetworkID)
        {
            return true;
        }

        public void RemoveNetwork()
        {
            //power would be lost, need to remove this from the master list, remove members etc
            foreach (FlexNetworkMember m in GetMembers())
            {
                if (m != null&&m.NetworkID==NetworkID) { m.NetworkRemove(); }
                
            }
            
        }

        public void OnTick(float dt)
        {
            poweruse = 0;
            powergen = 0;
            List<PowerNetworkMember> requestors = new List<PowerNetworkMember>();
            foreach (FlexNetworkMember m in GetMembers())
            {
                PowerNetworkMember pm = m as PowerNetworkMember;
                if (pm == null) { continue; }
                powergen += pm.AvailablePower();
                
                poweruse += pm.RequestPower();
                if (pm.RequestPower() > 0) { requestors.Add(pm); }
            }
            int powerusecounter = powergen;
            foreach (PowerNetworkMember puser in requestors)
            {
                
                powerusecounter-= puser.ReceivePowerOffer(powerusecounter);
                powerusecounter = Math.Max(0, powerusecounter);
            }
            networkstatus.generated = powergen;
            networkstatus.consumed = poweruse;
            networkstatus.nodes = GetMembers().Count();
        }
    }

    interface PowerNetworkMember : FlexNetworkMember
    {
        int AvailablePower();
        int RequestPower();
        int ReceivePowerOffer(int amt);//if sent 0 or less device will no it is unpowered
    }

    public class PowerNetworkInfo
    {
        public int generated;
        public int consumed;
        public int nodes;
        public PowerNetworkInfo()
        {
            generated = 0;
            consumed = 0;
            nodes = 0;
        }
        public PowerNetworkInfo(int generated,int consumed, int nodes)
        {
            this.generated = generated;
            this.consumed = consumed;
            this.nodes = nodes;
        }
    }
}
