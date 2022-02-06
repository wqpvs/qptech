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
        List<BlockPos> outputlocations;
        List<BlockFacing> inputfaces;
        List<BlockFacing> outputfaces;
        

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
            }
            if (Api is ICoreServerAPI) { RegisterGameTickListener(OnServerTick, 100); }
            r = new Random();
        }
        Random r;
        public virtual void OnServerTick(float dt)
        {
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
            foreach (BlockFacing facing in outputfaces)
            {
                BlockPos outpos = Pos.Copy().Offset(facing);
                IItemTransporter trans = Api.World.BlockAccessor.GetBlockEntity(outpos) as IItemTransporter;
                if (trans==null || !trans.CanAcceptItems(this)) { continue; }
                availableoutputs.Add(trans);
            }
            if (availableoutputs.Count() == 0) { return; }
            
            int pick = r.Next(0, availableoutputs.Count());
            int used = availableoutputs[pick].ReceiveItemStack(itemstack,this);
            itemstack.StackSize -= used;
            if (itemstack.StackSize <= 0) { ResetStack(); }
            else { MarkDirty(true); }
        }

        public virtual bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
           
            
            if (byPlayer.Entity.RightHandItemSlot.Itemstack.Item != null && byPlayer.Entity.RightHandItemSlot.Itemstack.Item.Code.ToString().Contains("wrench"))
            {
                if (Api is ICoreClientAPI)
                {
                    byte[] blockseldata = SerializerUtil.Serialize<BlockSelection>(blockSel);
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
                BlockSelection bs = SerializerUtil.Deserialize<BlockSelection>(data);
                string direction = bs.Face.ToString();
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

        //build the helper lists to mactch the current face setup
        protected virtual void SetIOLocations()
        {
            inputlocations = new List<BlockPos>();
            outputlocations = new List<BlockPos>();
            inputfaces = new List<BlockFacing>();
            outputfaces = new List<BlockFacing>();
            foreach (string direction in facesettings.Keys)
            {
                BlockFacing bf = BlockFacing.FromCode(direction);
                BlockPos usepos = Pos.Copy().Offset(bf);
                string state = facesettings[direction];
                if (state == faceinput) { inputlocations.Add(usepos); inputfaces.Add(bf); }
                else if (state== faceoutput) { outputlocations.Add(usepos); outputfaces.Add(bf); }
            }
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

            

        }
    }
}
