using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace qptech.src.networks
{
    class PowerNetwork : IFlexNetwork
    {
        Guid networkid;
        string productid="power";
        int powergen;
        int poweruse;
        int powerstored;
        public int PowerGeneration => powergen;
        public int PowerUse => poweruse;
        List<IFlexNetworkMember> members;
        public Guid NetworkID => networkid;
        public string ProductID => productid;
        PowerNetworkInfo networkstatus;
        public PowerNetworkInfo NetworkStatus=>networkstatus;
        public void OnPulse(string channel)
        {
            foreach (IPowerNetworkMember ipn in GetMembers().ToArray())
            {
                if (ipn != null) { ipn.OnPulse(channel); }
            }
        }
        public PowerNetwork(Guid g)
        {
            networkid = g;
            networkstatus = new PowerNetworkInfo();
            //SET NETWORK ID - need a master network manager
        }
        public List<IFlexNetworkMember> GetMembers()
        {
            if (members == null) { members = new List<IFlexNetworkMember>(); }
            return members;
        }
        public bool JoinNetwork(IFlexNetworkMember newmember)
        {
            if (newmember.ProductID != ProductID) { return false; }
            if (newmember as IPowerNetworkMember == null) { return false; }
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
            foreach (IFlexNetworkMember m in GetMembers())
            {
                if (m != null&&m.NetworkID==NetworkID) { m.NetworkRemove(); }
                
            }
            
        }

        public void OnTick(float dt)
        {
            poweruse = 0;
            powergen = 0;
            powerstored = 0;
            List<IPowerNetworkMember> requestors = new List<IPowerNetworkMember>();
            List<IPowerNetworkMember> batteries = new List<IPowerNetworkMember>();
            //EVALUATE POWER NETWORK
            foreach (IFlexNetworkMember m in GetMembers())
            {
                IPowerNetworkMember pm = m as IPowerNetworkMember;
                if (pm == null) { continue; }
                powergen += pm.AvailablePower();
                
                poweruse += pm.RequestPower();
                if (pm.RequestPower() > 0) { requestors.Add(pm); }

                
                if (pm.IsBattery) { batteries.Add(pm); powerstored += pm.AvailableStorage(); }
            }
            int powerusecounter = powergen;
            int batteryuse = 0;           
            
            foreach (IPowerNetworkMember puser in requestors)
            {
                int poweroffer = powerusecounter + powerstored-batteryuse;
                if (poweroffer <= 0) { poweroffer=0; }
                int powerused= puser.ReceivePowerOffer(poweroffer);
                powerusecounter -= powerused;
            }
            foreach (IPowerNetworkMember pnm in batteries)
            {
                if (powerusecounter == 0) { break; }
                else if (powerusecounter > 0)
                {
                    int stored = pnm.StorePower(powerusecounter);
                    powerusecounter -= stored;
                    if (powerusecounter < 0) { powerusecounter = 0; }
                }                    
                else
                {
                    int used = pnm.DrawStoredPower(-powerusecounter);
                    powerusecounter += used;
                    if (powerusecounter > 0) { powerusecounter = 0; }
                }
            }
            
            
            networkstatus.generated = powergen;
            networkstatus.consumed = poweruse;
            networkstatus.stored = powerstored;
            networkstatus.nodes = GetMembers().Count();
        }
    }

    interface IPowerNetworkMember : IFlexNetworkMember
    {
        bool IsBattery { get; }
        int AvailablePower();
        int AvailableStorage();
        int DrawStoredPower(int amt);
        int StorePower(int amt);
        int RequestPower();
        int ReceivePowerOffer(int amt);//if sent 0 or less device will no it is unpowered
    }

    public class PowerNetworkInfo
    {
        public int generated;
        public int consumed;
        public int nodes;
        public int stored;
        public PowerNetworkInfo()
        {
            generated = 0;
            consumed = 0;
            nodes = 0;
            stored = 0;
        }
        public PowerNetworkInfo(int generated,int consumed, int nodes, int stored)
        {
            this.generated = generated;
            this.consumed = consumed;
            this.nodes = nodes;
            this.stored = stored;
        }
    }
}
