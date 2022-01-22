using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VintagestoryAPI;
using VintagestoryAPI.Math;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Client;
using Vintagestory.API.Server;

namespace qptech.src.itemtransport
{
    interface IItemTransporter
    {
        int ReceiveItemStack(ItemStack incomingstack,IItemTransporter fromtransporter);
        bool CanAcceptItems();
        
        BlockPos TransporterPos { get; }
    }
}
