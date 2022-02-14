using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.API.Config;
namespace qptech.src.itemtransport
{
    class ItemPipe : BlockEntity, IItemTransporter
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
        public int StackSize => stacksize;
        public ItemFilter itemfilter;
        bool lockswap = false;
        bool showitems = true;
        bool optionshowitems = true;
        bool ShowItems => showitems&&optionshowitems;
        bool autofiltertocrate = true;
        protected virtual bool itemstackempty => itemstack == null || itemstack.StackSize == 0 || (itemstack.Item == null && itemstack.Block == null);

        public bool CanAcceptItems(IItemTransporter fromtransporter)
        {
            if (fromtransporter != null && fromtransporter.TransporterPos == outputlocation) { return false; }
            if (itemstack == null) { return true; }
            return false;
        }

        public virtual ItemFilter GetItemFilter()
        {
            return itemfilter;
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
                showitems = Block.Attributes["showitems"].AsBool(showitems);
            }
            if (Api is ICoreServerAPI) { RegisterGameTickListener(OnServerTick, 100); }
            else { RegisterGameTickListener(OnClientTick, 100); }
        }


        public int ReceiveItemStack(ItemStack incomingstack, IItemTransporter fromtransporter)
        {
            //TODO - should probably filter liquids
            if (fromtransporter != null && fromtransporter.TransporterPos != inputlocation) { return 0; }
            if (ItemStack == null) {
                int acceptqty= Math.Min(incomingstack.StackSize, stacksize);
                if (itemfilter != null)
                {
                    acceptqty = itemfilter.TestStack(incomingstack);
                    if (acceptqty == 0) { return 0; }
                }
                itemstack = incomingstack.Clone();
                itemstack.StackSize = acceptqty;
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
        
        public void OnClientTick(float dt)
        {
            GenMesh();
        }

        protected virtual void VerifyConnections()
        {
            //if it has connections, make sure they're still there
            //if there aren't any connections, check and see if a destination can be set and connect
            destination = null;
            if (outputlocation == null) { return; }
            IItemTransporter trans = Api.World.BlockAccessor.GetBlockEntity(outputlocation) as IItemTransporter;
            BlockEntityContainer outcont = Api.World.BlockAccessor.GetBlockEntity(outputlocation) as BlockEntityContainer;
            
            if (trans == null && outcont==null) {MarkDirty(true);return; }
            BlockEntityCrate crate = outcont as BlockEntityCrate;
            if (crate != null && autofiltertocrate && !crate.Inventory.Empty)
            {
                itemfilter = new ItemFilter();
                itemfilter.filtercode = crate.Inventory[0].Itemstack.Collectible.Code.ToString();
                MarkDirty();
            }
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
        //interesting code: return Lang.GetMatching(Code?.Domain + AssetLocation.LocationSeparator + "block-" + type + "-" + Code?.Path, Lang.Get("cratelidstate-" + lidState, "closed"));
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
            BlockEntityContainer outcont = Api.World.BlockAccessor.GetBlockEntity(outputlocation) as BlockEntityContainer;
            BlockEntityBarrel beb = Api.World.BlockAccessor.GetBlockEntity(outputlocation) as BlockEntityBarrel;
            if (beb != null) {

                TryBarrel(beb);
                return;
            }
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

        protected virtual void TryBarrel(BlockEntityBarrel barrel)
        {
            if (barrel == null||itemstackempty) { return; }
            if (barrel.Sealed||barrel.CanSeal) { return; } //if there is a valid barrel recipe then don't put anything in
            if (barrel.Inventory == null || barrel.Inventory.Count == 0) { return; }
            if (barrel.Inventory[0] == null || barrel.Inventory[0].Empty || barrel.Inventory[0].Itemstack.Item == null)
            {
                ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, itemstack.StackSize);
                DummyInventory dummy = new DummyInventory(Api,1);
                dummy[0].Itemstack = itemstack;
                dummy[0].TryPutInto(barrel.Inventory[0], ref op);
                if (op.MovedQuantity == 0) { return; }
                barrel.Inventory.MarkSlotDirty(0);
                if (op.NotMovedQuantity == 0) { itemstack = null;MarkDirty(true);return; }
                itemstack.StackSize = op.NotMovedQuantity;
                MarkDirty(true);
                return;
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
            if (inputlocation == null) { return; }
            BlockEntity temp = Api.World.BlockAccessor.GetBlockEntity(inputlocation);
            BlockEntityGenericTypedContainer incont = temp as BlockEntityGenericTypedContainer;
            if (incont == null || incont.Inventory == null || incont.Inventory.Empty) { return; }
            foreach(ItemSlot slot in incont.Inventory)
            {
                if (slot == null || slot.Empty||slot.Itemstack==null) { continue; }
                int takefiltered = Math.Min(stacksize, slot.Itemstack.StackSize);
                if (itemfilter != null)
                {
                    takefiltered = itemfilter.TestStack(slot.Itemstack);
                    takefiltered = Math.Min(stacksize, takefiltered);
                    
                    
                }
                if (takefiltered <= 0) { continue; }
                itemstack = slot.Itemstack.Clone();
                itemstack.StackSize = takefiltered;
                
                slot.Itemstack.StackSize -= takefiltered;
                if (slot.Itemstack.StackSize <= 0) { slot.Itemstack = null; }
                slot.MarkDirty();
                progress = 0;
                
                
                MarkDirty(true);
                break;
            }
        }
        MeshData meshdata;
        public virtual void GenMesh()
        {
            ICoreServerAPI sapi = Api as ICoreServerAPI;
            if (sapi != null)
            {
                sapi.Network.BroadcastBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, (int)enPacketIDs.GenMesh,null );
            }
            meshdata = null;
            ICoreClientAPI capi = Api as ICoreClientAPI;
            if (capi == null) { return; }
            if (itemstack == null || (itemstack.Item == null && itemstack.Block == null)) { return; }


            if (itemstack.Class == EnumItemClass.Item)
            {
                capi.Tesselator.TesselateItem(itemstack.Item, out meshdata);
            }
            else
            {
                capi.Tesselator.TesselateBlock(itemstack.Block, out meshdata);
            }

            float[] meshsize = GetMeshSize(meshdata);

            float scalefactor = Math.Max(Math.Max(meshsize[0], meshsize[1]), meshsize[2]);
            float targetscale = 0.5f;
            if (scalefactor <= 0) { scalefactor = targetscale; }
            else
            {
                scalefactor = targetscale / scalefactor; /// 0.5/1
            }
            Vec3f mid = new Vec3f(0.5f, 0.5f, 0.5f);
            scalefactor = Math.Min(scalefactor, 1);
            meshdata.Scale(mid, scalefactor, scalefactor, scalefactor);
            BlockPos o = new BlockPos(0, 0, 0);

            //Vec3f startv = o.Copy().Offset(outputface.Opposite).ToVec3f();
            Vec3f startv = Vec3f.Zero;
            Vec3f endv = o.Copy().Offset(outputface).ToVec3f();

            Vec3f nowv = Lerp(startv, endv, progress);
            nowv.Y += 0.15f;
            meshdata.Translate(nowv);
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            if (!showitems||meshdata==null) { return base.OnTesselation(mesher, tessThreadTesselator); }
            
            mesher.AddMeshData(meshdata);
            /*Shape displayshape = capi.TesselatorManager.GetCachedShape(new AssetLocation("machines:block/metal/electric/roundgauge0"));


            MeshData meshdata;
            capi.Tesselator.TesselateShape("roundgauge0" + Pos.ToString(), displayshape, out meshdata, this);



            meshdata.Translate(DisplayOffset());
            meshdata.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, GameMath.DEG2RAD * DisplayRotation(), 0);


            mesher.AddMeshData(meshdata);*/
            return base.OnTesselation(mesher, tessThreadTesselator);
        }

        public virtual bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            //if we have a filter and player clicks wiht right hand, clear the filter
            /*if (itemfilter != null && player.Entity.RightHandItemSlot.Itemstack == null)
            {
                (Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, (int)enPacketIDs.ClearFilter, null);
                return true;
            }
            else if (player.Entity.RightHandItemSlot.Itemstack != null)
            {
                itemfilter = new ItemFilter();
                ItemStack pstack = player.Entity.RightHandItemSlot.Itemstack;
                itemfilter.SetFilterToStack(pstack);
                byte[] filterasbytes = SerializerUtil.Serialize<ItemFilter>(itemfilter);
                (Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, (int)enPacketIDs.SetFilter, filterasbytes);
                return true;
            }
            */
            if (byPlayer.Entity.RightHandItemSlot.Itemstack == null)
            {
                OpenStatusGUI();
                return true;
            }
            else if (byPlayer.Entity.RightHandItemSlot.Itemstack.Item != null&&byPlayer.Entity.RightHandItemSlot.Itemstack.Item.Code.ToString().Contains("wrench"))
            {
                WrenchSwap();
                return true;
            }
            return false;
        }

        public virtual void WrenchSwap()
        {
            if (Api is ICoreClientAPI)
            {
                if (lockswap) { return; }
                lockswap = true;
                (Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, (int)enPacketIDs.WrenchSwap, null);
                return;
            }
            
            
            string dircode = Block.Code.ToString();
            bool doswap = false;
            if (dircode.Contains("-up")) { dircode = dircode.Replace("up", "down");doswap = true; }
            else if (dircode.Contains("-down")) { dircode = dircode.Replace("down", "north"); doswap = true; }
            else if (dircode.Contains("-north")) { dircode = dircode.Replace("north", "east"); doswap = true; }
            else if (dircode.Contains("-east")) { dircode = dircode.Replace("east","south"); doswap = true; }
            else if (dircode.Contains("-south")) { dircode = dircode.Replace("south", "west"); doswap = true; }
            else if (dircode.Contains("-west")) { dircode = dircode.Replace("west", "up"); doswap = true; }
            if (doswap)
            {
                Block newblock = Api.World.GetBlock(new AssetLocation(dircode));
                if (newblock != null)
                {
                    ItemFilter pushfilter=null;
                    if (itemfilter != null)
                    {
                        pushfilter = itemfilter.Copy();
                    }
                    Api.World.BlockAccessor.SetBlock(newblock.Id, Pos);
                    if (pushfilter != null)
                    {
                        ItemPipe newconveyor = Api.World.BlockAccessor.GetBlockEntity(Pos) as ItemPipe;
                        if (newconveyor == this)
                        {
                           
                        }
                        else
                        {
                            newconveyor.ServerSetupNew(pushfilter);

                        }
                    }
                }
            }
        }

        GUIConveyor gui;
        public virtual void OpenStatusGUI()
        {
            ICoreClientAPI capi = Api as ICoreClientAPI;


            if (capi != null)
            {
                if (gui == null)
                {
                    gui = new GUIConveyor("Conveyor Setup", Pos, capi);

                    gui.TryOpen();
                    gui.SetupDialog(this);

                }
                else
                {
                    gui.TryClose();
                    gui.TryOpen();
                    gui.SetupDialog(this);
                }
            }

        }
        public virtual void ServerSetupNew(ItemFilter newfilter)
        {
            itemfilter = newfilter.Copy();
            
            MarkDirty();
        }



        public enum enPacketIDs
        {
            ClearFilter = 99991001,
            SetFilter = 99991002,
            WrenchSwap = 99991003,
            ShowItemToggle = 99991004,
            GenMesh= 99991005
        }
        
        public void OnNewFilter(ItemFilter newfilter)
        {
            itemfilter = newfilter;
            
            byte[] filterasbytes = SerializerUtil.Serialize<ItemFilter>(itemfilter);
            (Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, (int)enPacketIDs.SetFilter, filterasbytes);
        }
        
        public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
        {
            if (packetid == (int)enPacketIDs.ClearFilter)
            {
                if (itemfilter != null)
                {
                    itemfilter.ClearFilter();
                    MarkDirty(true);
                }
            }
            else if (packetid == (int)enPacketIDs.SetFilter)
            {
                itemfilter = SerializerUtil.Deserialize<ItemFilter>(data);
                MarkDirty(true);
            }
            else if (packetid== (int)enPacketIDs.WrenchSwap)
            {
                WrenchSwap();
            }
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            base.OnReceivedServerPacket(packetid, data);
            if (packetid == (int)enPacketIDs.GenMesh) { GenMesh(); }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetFloat("progress", progress);
            tree.SetItemstack("itemstack", itemstack);
            if (destination == null) { destination = Pos; }
            tree.SetBlockPos("destination", destination);
            byte[] filterasbytes= SerializerUtil.Serialize<ItemFilter>(itemfilter);
            
            tree.SetBytes("itemfilter", filterasbytes);
            tree.SetBool("optionshowitems", optionshowitems);
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
            itemfilter = null;
           if (tree.HasAttribute("itemfilter"))
            {
                byte[] data = tree.GetBytes("itemfilter");
                if (data.Length > 0)
                {
                    itemfilter = SerializerUtil.Deserialize<ItemFilter>(data);
                }
            }
            optionshowitems = tree.GetBool("optionshowitems");
            
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

        public int CheckItemFilter(ItemStack inputstack)
        {
            return 0;
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            dsc.AppendLine("Item Transport");
            if (ItemStack != null) { dsc.AppendLine("Transporting " + itemstack.ToString() + " %" + progress); }
            
            if (destination!=null && destination != Pos) { dsc.AppendLine("To " + destination.ToString()); }
            if (itemfilter != null) { dsc.AppendLine(itemfilter.GetFilterDescription()); }
            dsc.AppendLine(Block.LastCodePart().ToString());

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

        public static float[] GetMeshSize(MeshData mesh)
        {
            float[] result = { 0, 0, 0 };
            if (mesh == null || mesh.xyz==null|| mesh.VerticesCount < 3) { return result; }
            float[] min = { 1000, 1000, 1000 };
            float[] max = { -1000, -1000, -1000 };
            for (int c = 0; c < mesh.VerticesCount; c++)
            {
                int xref = c * 3;int yref = xref + 1;int zref = yref + 1;
                min[0] = Math.Min(mesh.xyz[xref], min[0]);
                min[1] = Math.Min(mesh.xyz[yref], min[1]);
                min[2] = Math.Min(mesh.xyz[zref], min[2]);
                max[0] = Math.Max(mesh.xyz[xref], max[0]);
                max[1] = Math.Max(mesh.xyz[yref], max[1]);
                max[2] = Math.Max(mesh.xyz[zref], max[2]);
            }
            result[0] = max[0] - min[0];
            result[1] = max[1] - min[1];
            result[2] = max[2] - min[2];
            return result;
        }
    }
   
}
