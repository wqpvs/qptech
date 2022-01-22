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
    /// <summary>
    /// Item Splitter will take in items on its input items and output them as evenly (by stack) across
    /// its output faces. (Each time it outputs it will cycle thru its possible output faces)
    /// </summary>
    class ItemSplitter : BlockEntity, IItemTransporter
    {
        List<BlockFacing> inputfaces;
        List<BlockFacing> outputfaces;
        List<BlockPos> inputlocations;
        List<BlockPos> outputlocations;
        public BlockPos TransporterPos => Pos;
        ItemStack itemstack;
        int stacksize = 1000;
        

        public bool CanAcceptItems()
        {
            if (itemstack == null) { return true; }
            return false;
        }

        public int ReceiveItemStack(ItemStack incomingstack, IItemTransporter fromtransporter)
        {
            if (!CanAcceptItems()) { return 0; }
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
                string[] cfaces = { };
                if (!Block.Attributes.KeyExists("inputfaces")) { inputfaces = BlockFacing.ALLFACES.ToList<BlockFacing>(); }
                else
                {
                    cfaces = Block.Attributes["inputfaces"].AsArray<string>(cfaces);
                    inputfaces = new List<BlockFacing>();
                    foreach (string f in cfaces)
                    {
                        inputfaces.Add(BEElectric.OrientFace(Block.Code.ToString(), BlockFacing.FromCode(f)));
                    }
                }
                inputlocations = new List<BlockPos>();
                foreach (BlockFacing bf in inputfaces)
                {
                    inputlocations.Add(Pos.Copy().Offset(bf));
                }
                if (!Block.Attributes.KeyExists("outputfaces")) { outputfaces = BlockFacing.ALLFACES.ToList<BlockFacing>(); }
                else
                {
                    cfaces = Block.Attributes["outputfaces"].AsArray<string>(cfaces);
                    outputfaces = new List<BlockFacing>();
                    foreach (string f in cfaces)
                    {
                        outputfaces.Add(BEElectric.OrientFace(Block.Code.ToString(), BlockFacing.FromCode(f)));
                    }
                }
                outputlocations = new List<BlockPos>();
                foreach (BlockFacing bf in outputfaces)
                {
                    outputlocations.Add(Pos.Copy().Offset(bf));
                }
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
                if (trans==null || !trans.CanAcceptItems()) { continue; }
                availableoutputs.Add(trans);
            }
            if (availableoutputs.Count() == 0) { return; }
            
            int pick = r.Next(0, availableoutputs.Count());
            int used = availableoutputs[pick].ReceiveItemStack(itemstack,this);
            itemstack.StackSize -= used;
            if (itemstack.StackSize <= 0) { ResetStack(); }
            else { MarkDirty(true); }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetItemstack("itemstack", itemstack);
            
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            
            itemstack = tree.GetItemstack("itemstack");
            if (itemstack != null)
            {
                itemstack.ResolveBlockOrItem(worldAccessForResolve);
            }
            
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
