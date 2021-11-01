using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace qptechElectricity.API
{

    /// <summary>
    /// https://discord.gg/VbYQc7yfnF
    /// 
    /// Class to Distribute electricity between block entities
    /// note this is barebones - what connects to what, and whether it needs power, and to use up power
    /// you would have to create your own tick (default 75) to go through and distribute available power if applicable
    /// (or it can passively accept power)
    /// 
    /// 1.0.2 Major Refactor - will now deal with Temporal Flux or Flux units (TF Units)
    /// this is similar to how Minecraft mods typically deal in Redstone Flux or RF untis
    /// 
    /// Removing voltage all together - everything will be determined by how many TF your devices etc can handle
    /// 
    /// OnTick length to handle your electricity updates - 75
    /// </summary>

    // Class to Distribute electricity between block entities
    // note this is barebones - what connects to what, and whether it needs power, and to use up power
    // you would have to create your own tick (default 75) to go through and distribute available power if applicable
    // (or it can passively accept power)

    public interface IElectricity
    {
        ///the block entity this IElectricity belongs to
        BlockEntity EBlock { get; }
        ///MaxAmps - How much power can be transferred in one turn

        int MaxFlux { get;  }
        
        ///if device is on
        bool IsOn { get;  }
        ///Device has power
        bool IsPowered { get; }
        ///Receive an offer for a power packet, return how much power it uses
        int ReceivePacketOffer(IElectricity from, int inFlux);
        ///return if this object needs any power
        int NeedPower();
        ///Try to accept an IElectricity as a connected power source (return 0 to refuse connection)
        bool TryInputConnection(IElectricity connectto);
        ///Try to accept an IElectricity as a connected power destination
        bool TryOutputConnection(IElectricity connectto);
        ///Delete any connection the the supplied IElectricity (should be called on every connection when the block is deleted, removed etc)
        void RemoveConnection(IElectricity disconnect);
    }

}
