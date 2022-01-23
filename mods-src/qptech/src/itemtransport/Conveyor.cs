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
        BlockPos inputlocation;

        BlockFacing outputface=BlockFacing.EAST;
        public BlockFacing TransporterOutputFace => outputface;
        BlockPos outputlocation;
        public BlockPos TransporterPos => Pos;

        float transportspeed = 0.1f;

        int stacksize = 1;

        

        public bool CanAcceptItems(IItemTransporter fromtransporter)
        {
            if (fromtransporter != null && fromtransporter.TransporterPos == outputlocation) { return false; }
            if (itemstack == null) { return true; }
            return false;
        }
        
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (Block.Attributes != null)
            {
                inputface = BlockFacing.FromCode(Block.Attributes["inputface"].AsString("east"));
                outputface = BlockFacing.FromCode(Block.Attributes["outputface"].AsString("west"));
                inputface = BEElectric.OrientFace(Block.Code.ToString(), inputface);
                outputface = BEElectric.OrientFace(Block.Code.ToString(), outputface);
                inputlocation = Pos.Copy().Offset(inputface);
                outputlocation = Pos.Copy().Offset(outputface);
                transportspeed = Block.Attributes["transportspeed"].AsFloat(transportspeed);
                stacksize = Block.Attributes["stacksize"].AsInt(stacksize);
            }
            if (Api is ICoreServerAPI) { RegisterGameTickListener(OnServerTick, 100); }

        }


        public int ReceiveItemStack(ItemStack incomingstack, IItemTransporter fromtransporter)
        {
            //TODO - should probably filter liquids
            if (fromtransporter != null && fromtransporter.TransporterPos != inputlocation) { return 0; }
            if (ItemStack == null) {
                itemstack = incomingstack.Clone();
                itemstack.StackSize = Math.Min(itemstack.StackSize, stacksize);
                progress = 0;
                MarkDirty(true);
                return itemstack.StackSize;
            }
            return 0;
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
            destination = null;
            IItemTransporter trans = Api.World.BlockAccessor.GetBlockEntity(outputlocation) as IItemTransporter;
            BlockEntityGenericTypedContainer outcont = Api.World.BlockAccessor.GetBlockEntity(outputlocation) as BlockEntityGenericTypedContainer;
            if (trans == null && outcont==null) {MarkDirty(true);return; }
            destination = outputlocation;
            
            MarkDirty(true);
        }

        protected virtual void HandleStack()
        {
            //if there is a destination and an item stack, handle movement, trigger rendering if necessary
            //if movement is complete handle transfer to destination
            Random r = new Random();
            
            if (itemstack == null)
            {
                TryTakeStack();
                return;
            }
            if (destination==null) { return; }
            IItemTransporter trans = Api.World.BlockAccessor.GetBlockEntity(outputlocation) as IItemTransporter;
            if (trans !=null && !trans.CanAcceptItems(this)) { return; } // we are connected to transporter but it's busy
            
            //if all is well then update the progress
            progress += transportspeed;
            progress = Math.Min(progress,1);
            //if we've moved everything, attempt to hand off stack
            if (progress >= 1) { TransferStack(); }
        }

        protected virtual void TransferStack()
        {
            if (itemstack == null) { return; }
            //attempt to transfer to another transporter
            IItemTransporter trans = Api.World.BlockAccessor.GetBlockEntity(outputlocation) as IItemTransporter;
            if (trans!=null)
            {
                int giveamount = trans.ReceiveItemStack(itemstack,this);
                if (giveamount > 0)
                {
                    itemstack.StackSize -= giveamount;
                    progress = 0;
                    if (itemstack.StackSize <= 0) { ResetStack();return; }
                    MarkDirty(true);
                }
            }
            BlockEntityGenericTypedContainer outcont = Api.World.BlockAccessor.GetBlockEntity(outputlocation) as BlockEntityGenericTypedContainer;
            if (outcont == null) { return; }
            if (outcont.Inventory == null) { return; }
            DummyInventory dummy = new DummyInventory(Api,1);
            dummy[0].Itemstack = itemstack;
            WeightedSlot tryoutput = outcont.Inventory.GetBestSuitedSlot(dummy[0]);

            if (tryoutput.slot != null)
            {
                int ogqty = itemstack.StackSize;
                ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, itemstack.StackSize);

                dummy[0].TryPutInto(tryoutput.slot, ref op);
                
                if (op.MovedQuantity > 0) {
                    outcont.MarkDirty();
                    
                }
                if (op.NotMovedQuantity == 0||itemstack.StackSize==0)
                {
                    ResetStack();
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
            BlockEntity temp = Api.World.BlockAccessor.GetBlockEntity(inputlocation);
            BlockEntityGenericTypedContainer incont = temp as BlockEntityGenericTypedContainer;
            if (incont == null || incont.Inventory == null || incont.Inventory.Empty) { return; }
            foreach(ItemSlot slot in incont.Inventory)
            {
                if (slot == null || slot.Empty) { continue; }
                itemstack = slot.Itemstack.Clone();
                itemstack.StackSize = Math.Min(stacksize, itemstack.StackSize);

                slot.Itemstack.StackSize -= itemstack.StackSize;
                if (slot.Itemstack.StackSize <= 0) { slot.Itemstack = null; }
                slot.MarkDirty();
                progress = 0;
                MarkDirty(true);
            }
        }
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {

            ICoreClientAPI capi = Api as ICoreClientAPI;
            if (capi == null) { return false; }
            if (itemstack == null) { return base.OnTesselation(mesher, tessThreadTesselator); }
            MeshData meshdata;
            
            if (itemstack.Class == EnumItemClass.Item)
            {
                capi.Tesselator.TesselateItem(itemstack.Item, out meshdata);
            }
            else
            {
                capi.Tesselator.TesselateBlock(itemstack.Block, out meshdata);
            }
            
            Vec3f mid = new Vec3f(0.5f, 0.5f, 0.5f);
            meshdata.Scale(mid, 0.5f, 0.5f, 0.5f);
            BlockPos o = new BlockPos(0, 0, 0);

            //Vec3f startv = o.Copy().Offset(outputface.Opposite).ToVec3f();
            Vec3f startv = Vec3f.Zero;
            Vec3f endv = o.Copy().Offset(outputface).ToVec3f();
            Vec3f nowv = Lerp(startv, endv, progress);
            meshdata.Translate(nowv);
            mesher.AddMeshData(meshdata);
            /*Shape displayshape = capi.TesselatorManager.GetCachedShape(new AssetLocation("machines:block/metal/electric/roundgauge0"));


            MeshData meshdata;
            capi.Tesselator.TesselateShape("roundgauge0" + Pos.ToString(), displayshape, out meshdata, this);



            meshdata.Translate(DisplayOffset());
            meshdata.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, GameMath.DEG2RAD * DisplayRotation(), 0);


            mesher.AddMeshData(meshdata);*/
            return base.OnTesselation(mesher, tessThreadTesselator);
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
        public override void OnBlockRemoved()
        {
            if (Api is ICoreAPI && itemstack != null)
            {
                DumpInventory();
            }
            base.OnBlockRemoved();
        }

        protected virtual void DumpInventory()
        {
            if (itemstack == null || itemstack.StackSize == 0) { return; }
            DummyInventory di = new DummyInventory(Api,1);
            di[0].Itemstack = itemstack;
            di.DropAll(Pos.Offset(BlockFacing.UP).ToVec3d());
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            dsc.AppendLine("Item Transport");
            if (ItemStack != null) { dsc.AppendLine("Transporting " + itemstack.ToString() + " %" + progress); }
            
            if (destination!=null && destination != Pos) { dsc.AppendLine("To " + destination.ToString()); }

        }
        //Lerp two vectors, but i'm not 100% this is mathematically correct way to do it
        public static Vec3f Lerp(Vec3f start, Vec3f end, float percent)
        {
            percent = Math.Min(percent, 1);
            percent = Math.Max(percent, 0);
            Vec3f output = start;
            output.X = start.X + (end.X - start.X) * percent;
            output.Y = start.Y + (end.Y - start.Y) * percent;
            output.Z = start.Z + (end.Z - start.Z) * percent;

            return output;
        }
    }
}
