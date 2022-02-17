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
using Vintagestory.API.Util;

namespace qptech.src.itemtransport
{
    /// <summary>
    /// Item Splitter will take in items on its input items and output them as evenly (by stack) across
    /// its output faces. (Each time it outputs it will cycle thru its possible output faces)
    /// </summary>
    class ItemSplitter : BlockEntity, IItemTransporter
    {
        

        Dictionary<string, string> facesettings;
        public BlockPos TransporterPos => Pos;
        ItemStack itemstack;
        int stacksize = 1000;
        List<BlockPos> inputlocations;
        public List<BlockPos> InputLocations => inputlocations;
        List<BlockPos> outputlocations;
        public List<BlockPos> OutputLocations => outputlocations;
        List<BlockFacing> inputfaces;
        List<BlockFacing> outputfaces;
        string entranceshape= "machines:itemfilterentrance";
        string exitshape= "machines:itemfilterexit";
        Block entranceshapeblock;
        Block exitshapeblock;
        Dictionary<BlockPos, int> outputtracker;
        
        public virtual ItemFilter GetItemFilter() { return null; }

        public bool CanAcceptItems(IItemTransporter fromtransporter)
        {
            if (inputlocations == null) { return false; }
            if (fromtransporter != null && !inputlocations.Contains(fromtransporter.TransporterPos)) { return false; }
            if (itemstack == null) { return true; }

            return false;
        }

        public int ReceiveItemStack(ItemStack incomingstack, IItemTransporter fromtransporter)
        {
            if (itemstack != null) { return 0; }
            if (fromtransporter != null && !inputlocations.Contains(fromtransporter.TransporterPos)) { return 0; }
            itemstack = incomingstack.Clone();
            itemstack.StackSize = Math.Min(itemstack.StackSize, stacksize);
            MarkDirty(true);
            return itemstack.StackSize;
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (Block.Attributes != null)
            {
               
                stacksize = Block.Attributes["stacksize"].AsInt(stacksize);
                entranceshape = Block.Attributes["entranceshape"].AsString(entranceshape);
                exitshape = Block.Attributes["exitshape"].AsString(exitshape);
                entranceshapeblock = api.World.BlockAccessor.GetBlock(new AssetLocation(entranceshape));
                exitshapeblock = api.World.BlockAccessor.GetBlock(new AssetLocation(exitshape));
                
            }
            if (Api is ICoreServerAPI) { RegisterGameTickListener(OnServerTick, 100); }

            r = new Random();
        }
        Random r;
        public virtual void OnServerTick(float dt)
        {
            if (doautoconnect) { AutoConnect(); }
            if (itemstack == null) { return; }
            HandleItemStack();
        }
        protected virtual void ResetStack()
        {
            itemstack = null;
            MarkDirty(true);
        }
        protected virtual void HandleItemStack()
        {
            List<IItemTransporter> availableoutputs = new List<IItemTransporter>();
            foreach (BlockPos outpos in outputlocations)
            {
                //BlockPos outpos = Pos.Copy().Offset(facing);
                IItemTransporter trans = Api.World.BlockAccessor.GetBlockEntity(outpos) as IItemTransporter;
                
                if (trans==null || !trans.CanAcceptItems(this)) { continue; }
                if (trans.GetItemFilter() != null)
                {
                    //special case exact item filter match
                    if (trans.GetItemFilter().filtercode == itemstack.Collectible.Code.ToString())
                    {
                        int specialused = trans.ReceiveItemStack(itemstack, this);
                        if (specialused > 0)
                        {
                            itemstack.StackSize -= specialused;
                            if (itemstack.StackSize <= 0) { ResetStack(); }
                            MarkDirty();
                            return;
                        }
                    }
                }
                availableoutputs.Add(trans);
            }
            if (availableoutputs.Count() == 0) { return; }
            int pick = 0; int pickqty = 1000000;
            for (int c = 0; c < availableoutputs.Count; c++)
            {
                BlockPos checkpos = availableoutputs[c].TransporterPos;
                if (outputtracker.ContainsKey(checkpos)) { 
                    if (outputtracker[checkpos] < pickqty)
                    {
                        pickqty = outputtracker[checkpos];
                        pick = c;
                        

                    }
                }
                
            }
            
            int used = availableoutputs[pick].ReceiveItemStack(itemstack,this);
            outputtracker[availableoutputs[pick].TransporterPos] += 1;
            itemstack.StackSize -= used;
            if (itemstack.StackSize <= 0) { ResetStack(); }
            else { MarkDirty(true); }
        }

        public virtual bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
           
            
            if (byPlayer.Entity.RightHandItemSlot.Itemstack!=null&&byPlayer.Entity.RightHandItemSlot.Itemstack.Item != null && byPlayer.Entity.RightHandItemSlot.Itemstack.Item.Code.ToString().Contains("wrench"))
            {
                if (Api is ICoreClientAPI)
                {
                    BlockFacing bf = BEFluidPipe.GetSubFace(blockSel);
                    
                    byte[] blockseldata = SerializerUtil.Serialize<string>(bf.ToString());
                    (Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, (int)enPacketIDs.WrenchSwap, blockseldata);
                    return true;
                }
                return true;
            }
            return false;
        }

        public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
        {
            if (packetid == (int)enPacketIDs.WrenchSwap) {
                string direction = SerializerUtil.Deserialize<string>(data);
                
                if (facesettings[direction] == faceoff) { facesettings[direction] = faceinput; }
                else if (facesettings[direction] == faceinput) { facesettings[direction] = faceoutput; }
                else if (facesettings[direction] == faceoutput) { facesettings[direction] = faceoff; }
                SetIOLocations();
                MarkDirty(true);

            }
        }


        public enum enPacketIDs
        {
            WrenchSwap = 99990003
        }
        public const string faceoff = "";
        public const string faceinput = "i";
        public const string faceoutput = "o";

        //Set all faces to "off"
        protected virtual void ResetFaces()
        {
            facesettings = new Dictionary<string, string>();
            foreach (BlockFacing bf in BlockFacing.ALLFACES)
            {
                facesettings[bf.ToString()] = faceoff;
            }
           
        }

        bool doautoconnect = false;
        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);
            if (Api is ICoreServerAPI) { doautoconnect = true; }
        }
        
        public virtual void AutoConnect()
        {
            doautoconnect = false;
            if (facesettings == null) { SetIOLocations(); }
            bool anychanges = false;
            foreach (BlockFacing bf in BlockFacing.ALLFACES)
            {
                BlockPos bp = Pos.Copy().Offset(bf);
                BlockEntity be = Api.World.BlockAccessor.GetBlockEntity(bp);
                if (be == null) { continue; }
                ItemPipe ipipe = be as ItemPipe;
                if (ipipe != null)
                {
                    if (ipipe.InputLocation == Pos) { facesettings[bf.ToString()] = faceoutput; anychanges = true; }
                    else if (ipipe.OutputLocation == Pos) { facesettings[bf.ToString()] = faceinput;anychanges = true; }
                    continue;
                }
                ItemSplitter isplit = be as ItemSplitter;
                if (isplit != null)
                {
                    if (isplit.OutputLocations != null && isplit.OutputLocations.Contains(Pos)) { facesettings[bf.ToString()] = faceinput; anychanges = true; }
                    else if (isplit.InputLocations != null && isplit.InputLocations.Contains(Pos)) { facesettings[bf.ToString()] = faceoutput; anychanges = true; }
                }
            }
            if (anychanges) { SetIOLocations(); MarkDirty(true); }
        }

        //build the helper lists to mactch the current face setup
        protected virtual void SetIOLocations()
        {
            inputlocations = new List<BlockPos>();
            outputlocations = new List<BlockPos>();
            inputfaces = new List<BlockFacing>();
            outputfaces = new List<BlockFacing>();
            outputtracker = new Dictionary<BlockPos, int> ();

            foreach (string direction in facesettings.Keys)
            {
                BlockFacing bf = BlockFacing.FromCode(direction);
                BlockPos usepos = Pos.Copy().Offset(bf);
                string state = facesettings[direction];
                if (state == faceinput) { inputlocations.Add(usepos); inputfaces.Add(bf); }
                else if (state== faceoutput) { outputlocations.Add(usepos); outputfaces.Add(bf);outputtracker[usepos] = 0; }
            }
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            
            ICoreClientAPI capi = Api as ICoreClientAPI;
            if (capi == null) { return false; }
            
            MeshData mesh= new MeshData();
            if (facesettings == null) { ResetFaces();SetIOLocations(); }
            foreach (string direction in facesettings.Keys)
            {
                if (facesettings[direction] == faceoff) { continue; }
                Block useblock = entranceshapeblock;
                if (facesettings[direction] == faceoutput) { useblock = exitshapeblock; }
                capi.Tesselator.TesselateBlock(useblock,out mesh);
                if (direction == "north")
                {
                    mesher.AddMeshData(mesh);

                    //do nothing, the block is setup how we want it
                }
                else if (direction == "east")
                {
                    mesher.AddMeshData(mesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, GameMath.DEG2RAD * 270, 0));

                }
                else if (direction == "south")
                {
                    mesher.AddMeshData(mesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, GameMath.DEG2RAD * 180, 0));
                }
                else if (direction == "west")
                {
                    mesher.AddMeshData(mesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, GameMath.DEG2RAD * 90, 0));
                }
                else if (direction == "up")
                {

                    mesher.AddMeshData(mesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), GameMath.DEG2RAD * 90, 0, 0));

                }
                else if (direction == "down")
                {
                    mesher.AddMeshData(mesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), -GameMath.DEG2RAD * 90, 0, 0));

                }
                
            }
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
           
            tree.SetItemstack("itemstack", itemstack);
            if (facesettings == null || facesettings.Count() == 0) { ResetFaces(); }
            byte[] facesettingsdata = SerializerUtil.Serialize<Dictionary<string, string>>(facesettings);
            tree.SetBytes("facesettings", facesettingsdata);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            
            itemstack = tree.GetItemstack("itemstack");
            if (itemstack != null)
            {
                itemstack.ResolveBlockOrItem(worldAccessForResolve);
            }
            byte[] facesettingsdata = tree.GetBytes("facesettings");
            if (facesettingsdata == null || facesettingsdata.Length == 0) { ResetFaces(); }
            else { facesettings = SerializerUtil.Deserialize<Dictionary<string, string>>(facesettingsdata);  }
            SetIOLocations();
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
            DummyInventory di = new DummyInventory(Api, 1);
            di[0].Itemstack = itemstack;
            di.DropAll(Pos.Offset(BlockFacing.UP).ToVec3d());
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            dsc.AppendLine("Item Splitter");
            if (itemstack != null) { dsc.AppendLine("Transporting " + itemstack.ToString() ); }
            if (facesettings == null) { dsc.AppendLine("No Face Settings"); }
            else
            {
                dsc.AppendLine("Face settings:");
                foreach (string dir in facesettings.Keys)
                {
                    dsc.Append(dir + " " + facesettings[dir]+",");
                }
            }
            

        }
    }
}
