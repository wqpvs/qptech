using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Client;
using Vintagestory.API.Server;

namespace qptech.src.itemtransport
{
    class Conveyor : BlockEntity, IItemTransporter
    {
        IItemTransporter destination;
        public IItemTransporter Destination => destination;

        IItemTransporter source;
        public IItemTransporter Source => source;

        ItemStack itemstack;
        public ItemStack ItemStack => itemstack;

        float progress;
        public float Progress => progress;

        BlockFacing inputface;
        public BlockFacing TransporterInputFace => inputface;

        BlockFacing outputface;
        public BlockFacing TransporterOutputFace => outputface;

        public BlockPos TransporterPos => Pos;

        float transportspeed = 0.01f;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
        }

        public bool ConnectSource(IItemTransporter newsource)
        {
            if (ItemStack != null) { return false; }

            return true;
        }

        public bool ReceiveItemStack(ItemStack incomingstack)
        {
            if (ItemStack == null) { itemstack = incomingstack; MarkDirty(true); return true; }
            return false;
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if (ItemStack != null) { dsc.AppendLine("Transporting " + itemstack.ToString() + " %" + progress); }
            if (source != null) { dsc.AppendLine("From " + source.TransporterPos.ToString()); }
            if (destination != null) { dsc.AppendLine("To " + destination.TransporterPos.ToString()); }

        }
    }
}
