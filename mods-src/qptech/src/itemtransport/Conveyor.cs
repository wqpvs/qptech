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
        BlockPos destination;
        public BlockPos Destination => destination;
                

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

        protected virtual BlockPos CheckOutPos => Pos.Copy().Offset(outputface); //shortcut to check block at outputface
        protected virtual BlockPos CheckInPos => Pos.Copy().Offset(inputface);

        public bool CanAcceptItems()
        {
            if (itemstack == null) { return true; }
            return false;
        }
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            inputface = BlockFacing.FromCode(Block.Attributes["inputFace"].AsString("up"));
            outputface = BlockFacing.FromCode(Block.Attributes["outputFace"].AsString("down"));
            inputface = BEElectric.OrientFace(Block.Code.ToString(), inputface);
            outputface = BEElectric.OrientFace(Block.Code.ToString(), outputface);
            if (api is ICoreServerAPI) { RegisterGameTickListener(OnServerTick, 100); }
        }

        public bool ConnectSource(IItemTransporter newsource)
        {
            if (ItemStack != null) { return false; }

            return true;
        }

        public bool ReceiveItemStack(ItemStack incomingstack)
        {
            //TODO - should probably filter liquids
            if (ItemStack == null) { itemstack = incomingstack; progress = 0;  MarkDirty(true); return true; }
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
            if (destination != null) { return; }
            
            IItemTransporter trans = Api.World.BlockAccessor.GetBlockEntity(CheckOutPos) as IItemTransporter;
            
            if (trans == null) { return; }
            destination = CheckOutPos;
            
            MarkDirty(true);
        }

        protected virtual void HandleStack()
        {
            //if there is a destination and an item stack, handle movement, trigger rendering if necessary
            //if movement is complete handle transfer to destination
            if (itemstack == null)
            {
                TryTakeStack();
                return;
            }
            if (destination==Pos) { return; }
            IItemTransporter trans = Api.World.BlockAccessor.GetBlockEntity(CheckOutPos) as IItemTransporter;
            if (trans !=null && !trans.CanAcceptItems()) { return; } // we are connected to transporter but it's busy
            //if all is well then update the progress
            progress += transportspeed;
            progress = Math.Max(progress,1);
            //if we've moved everything, attempt to hand off stack
            if (progress == 1) { TransferStack(); }
        }

        protected virtual void TransferStack()
        {
            if (itemstack == null || destination == Pos) { return; }
            //attempt to transfer to another transporter
            IItemTransporter trans = Api.World.BlockAccessor.GetBlockEntity(CheckOutPos) as IItemTransporter;
            if (trans.ReceiveItemStack(itemstack))
            {
                ResetStack();
                return;
            }
            BlockEntityGenericContainer outcont = Api.World.BlockAccessor.GetBlockEntity(CheckOutPos) as BlockEntityGenericContainer;
            if (outcont == null) { return; }
            if (outcont.Inventory == null) { return; }
            DummyInventory dummy = new DummyInventory(Api,1);
            dummy[0].Itemstack = itemstack;
            WeightedSlot tryoutput = outcont.Inventory.GetBestSuitedSlot(dummy[0]);

            if (tryoutput.slot != null)
            {
                int ogqty = itemstack.StackSize;
                ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, itemstack.StackSize);

                int left=dummy[0].TryPutInto(tryoutput.slot, ref op);
                if (op.MovedQuantity > 0) {
                    outcont.MarkDirty();
                    if (left == 0) { ResetStack(); }
                    else { MarkDirty(true); }
                }
                
            }

        }

        protected virtual void ResetStack()
        {
            itemstack = null;
            progress = 0;
            MarkDirty(true);
        }

        protected virtual void TryTakeStack()
        {
            if (itemstack != null) { return; }
            BlockEntityGenericContainer incont = Api.World.BlockAccessor.GetBlockEntity(CheckInPos) as BlockEntityGenericContainer;
            if (incont == null || incont.Inventory == null || incont.Inventory.Empty) { return; }
            foreach(ItemSlot slot in incont.Inventory)
            {
                if (slot == null || slot.Empty) { continue; }
                itemstack = slot.Itemstack.Clone();
                slot.Itemstack = null;
                incont.MarkDirty();
                progress = 0;
                MarkDirty(true);
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetFloat("progress", progress);
            tree.SetItemstack("itemstack", itemstack);
            if (destination == null) { destination = Pos; }
            tree.SetBlockPos("destination", destination);
            
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            progress = tree.GetFloat("progress",0);
            itemstack = tree.GetItemstack("itemstack");
            if (itemstack != null)
            {
                itemstack.ResolveBlockOrItem(worldAccessForResolve);
            }
            destination = tree.GetBlockPos("destination",Pos);
            
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if (ItemStack != null) { dsc.AppendLine("Transporting " + itemstack.ToString() + " %" + progress); }
            
            if (destination != Pos) { dsc.AppendLine("To " + destination.ToString()); }

        }

    }
}
