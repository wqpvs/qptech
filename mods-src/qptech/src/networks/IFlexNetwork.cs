using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace qptech.src.networks
{
    interface IFlexNetworkMember
    {
        Guid NetworkID { get; }
        Guid MemberID { get; }
        string ProductID { get; }
        void NetworkRemove();
        void NetworkJoin(Guid newnetwork);
        Guid GetNetworkID(BlockPos requestedby, string fortype);
        
        void OnPulse(string channel);
    }
    interface IFlexNetwork
    {
        Guid NetworkID { get; } //the main reference id for all networks
        string ProductID { get; } //the product id for compatible networks - eg "power" 

        List<IFlexNetworkMember> GetMembers();
        
        bool JoinNetwork(IFlexNetworkMember member);
        void RemoveNetwork(); //remove all members deal with any inventory
        void OnTick(float dt);
        bool MergeWith(Guid newnetworkID); //
        void OnPulse(string channel);
        
    }    
}
