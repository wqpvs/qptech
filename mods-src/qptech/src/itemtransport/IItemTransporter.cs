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
        IItemTransporter Destination { get; }
        IItemTransporter Source { get; }
        ItemStack ItemStack { get; }
        float Progress { get; } //0-started, 1-finished
        bool ReceiveItemStack(ItemStack incomingstack);
        bool ConnectSource(IItemTransporter newsource);
        BlockFacing TransporterInputFace { get; }
        BlockFacing TransporterOutputFace { get; }
        BlockPos TransporterPos { get; }
    }
}
