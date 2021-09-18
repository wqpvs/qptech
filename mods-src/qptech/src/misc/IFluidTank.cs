using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace qptech.src
{
    public interface IFluidTank
    {
        int CapacityLitres { get; set; }
        int CurrentLevel { get;  }
        
        int ReceiveFluidOffer(Item offeredItem, int offeredAmount);
    }
}
