using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using qptech.src.networks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace qptech.src
{
    /// <summary>
    /// Simple electric crucible - gets nuggets, heats, and exports ingots
    /// BEElectric Crucible will be the larger version with complex interface etc
    /// </summary>
    class BEECrucible:BEEBaseDevice
    {
        //check input chest for nuggets
        //if found (and enough there) look up the combustiblePropsByType.meltingPoint
        //check if enough heat supplied (firepit, or Industiral Process?)
        //if there is enough then take the nuggets begin processing
        //output ingot when done, heated appropriately

    }
}
