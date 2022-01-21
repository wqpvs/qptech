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

        float progress=0;
        public float Progress => progress;

        BlockFacing inputface=BlockFacing.WEST;
        public BlockFacing TransporterInputFace => inputface;

        BlockFacing outputface=BlockFacing.EAST;
        public BlockFacing TransporterOutputFace => outputface;

        public BlockPos TransporterPos => Pos;

        float transportspeed = 0.01f;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api is ICoreServerAPI) { RegisterGameTickListener(OnServerTick, 100); }
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
        public void OnServerTick(float dt)
        {
            VerifyConnections();
            HandleStack();
        }

        protected virtual void VerifyConnections()
        {
            //if it has connections, make sure they're still there
            //if there aren't any connections, check and see if a destination can be set and connect
        }

        protected virtual void HandleStack()
        {
            //if there is a destination and an item stack, handle movement, trigger rendering if necessary
            //if movement is complete handle transfer to destination
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetFloat("progress", progress);
            tree.SetItemstack("itemstack", itemstack);
            if (Destination == null) { tree.SetBlockPos("destpos", Pos); }
            else { tree.SetBlockPos("destpos", Destination.TransporterPos); }
            if (Source == null) { tree.SetBlockPos("sourcepos", Pos); }
            else { tree.SetBlockPos("sourcepos", source.TransporterPos); }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            progress = tree.GetFloat("progress");
            itemstack = tree.GetItemstack("itemstack");
            if (itemstack != null)
            {
                itemstack.ResolveBlockOrItem(worldAccessForResolve);
            }
            BlockPos sourcepos = tree.GetBlockPos("sourcepos");
            if (sourcepos==null || sourcepos == Pos) { source = null; }
            else
            {
                source = Api.World.BlockAccessor.GetBlockEntity(sourcepos) as IItemTransporter;
            }
            BlockPos destpos = tree.GetBlockPos("destpos");
            if (destpos == null || destpos == Pos) { destination = null; }
            else
            {
                destination = Api.World.BlockAccessor.GetBlockEntity(destpos) as IItemTransporter;
            }
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
