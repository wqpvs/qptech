using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.API.Server;

namespace qptech.src.networks
{
    interface IFluidNetworkUser
    {
        //returns amount of fluid used or zero
        int OfferFluid(Item item, int quantity);
        //returns amount of desired fluid 
        int QueryFluid(Item item);
        //attempts to take fluid of type, (user returns amount supplied and adjusts its inventory)
        int TakeFluid(Item item, int quantity);
        //returns any fluid item that might be stored
        Item QueryFluid();

        bool IsOnlySource();
        bool IsOnlyDestination();
    }
}
