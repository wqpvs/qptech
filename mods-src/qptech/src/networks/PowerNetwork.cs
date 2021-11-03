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
        string networkid;
        string productid="power";
        int availablePower;
        public int Power => availablePower;
        List<FlexNetworkMember> members;
        public string NetworkID => networkid;
        public string ProductID => productid;
        public PowerNetwork()
        {
            //SET NETWORK ID - need a master network manager
        }
        public List<FlexNetworkMember> GetMembers()
        {
            if (members == null) { members = new List<FlexNetworkMember>(); }
            return members;
        }

        public bool MergeWith(string newnetworkID)
        {
            return true;
        }

        public void RemoveNetwork()
        {
            //power would be lost, need to remove this from the master list, remove members etc
        }

        public void PowerTick()
        {
            //query all power producers and add up available power
            //query all power users and supply power *if* there's enough power
            //I guess at this point would also set electrical devices to be powered or not
        }
    }
}
