﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace qptech.src.networks
{
    interface FlexNetworkMember
    {
        Guid NetworkID { get; }
        string ProductID { get; }
        void NetworkRemove();
        void NetworkJoin(string newnetwork);
        string GetNetworkID(BlockPos requestedby, string fortype);

    }
    interface FlexNetwork
    {
        Guid NetworkID { get; } //the main reference id for all networks
        string ProductID { get; } //the product id for compatible networks - eg "power" 

        List<FlexNetworkMember> GetMembers();
        
        void RemoveNetwork(); //remove all members deal with any inventory
        
        bool MergeWith(Guid newnetworkID); //

    }    
}
