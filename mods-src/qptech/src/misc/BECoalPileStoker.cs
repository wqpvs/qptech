using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Server;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;
using qptech.src.networks;
using qptech.src.itemtransport;

namespace qptech.src.misc
{
    /// <summary>
    /// Meant to keep coal piles lit, will add various checks for cementation furnace
    /// will change any fuel units into coke
    /// </summary>
    class BECoalPileStoker : BlockEntity, IItemTransporter
    {
        int fuel = 0; //how much fuel we have right now
        int maxfuel = 4; //how much fuel we can store
        int fuelincrement = 2; //how much fuel to insert at once
        bool switchOn = true; //only run if on
        public bool IsOn => switchOn;
        bool autoIgnite = true;
        bool coffinReady = false;
        bool ShouldIgnite => autoIgnite&&coffinReady;
        string[] fuelcodes = { "game:coke", "game:charcoal" };
        public virtual ItemFilter GetItemFilter()
        {
            return null;
        }
        BlockEntityStoneCoffin coffin; //find an active coffin for the cementation furnace
        List<Item> fuelitems;
        public BlockPos TransporterPos => Pos;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            fuelitems = new List<Item>();
            foreach (string code in fuelcodes)
            {
                fuelitems.Add(api.World.GetItem(new AssetLocation(code)));
            }
            if (api is ICoreServerAPI) { RegisterGameTickListener(OnServerTick, 500); }

        }
        #region IItemTransporter
        public bool CanAcceptItems(IItemTransporter fromtransporter)
        {
            if (IsOn&&fuel < maxfuel) { return true; }
            return false;
        }

        
        

        public int ReceiveItemStack(ItemStack incomingstack, IItemTransporter fromtransporter)
        {
            if (!IsOn) { return 0; }
            if (fuel >= maxfuel) { return 0; }
            if (incomingstack == null || !fuelitems.Contains(incomingstack.Item) || incomingstack.StackSize == 0) { return 0; }
            int used = Math.Min(maxfuel - fuel, incomingstack.StackSize);
            fuel += used; MarkDirty();
            return used;
        }
        public bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (byPlayer.Entity.RightHandItemSlot.Itemstack != null && byPlayer.Entity.RightHandItemSlot.Itemstack.Item != null && byPlayer.Entity.RightHandItemSlot.Itemstack.Item.Code.ToString().Contains("wrench"))
            {
                WrenchSwap();
                return true;
            }
            return true;
        }
        #endregion
        #region networkandinteraction

        public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
        {

            if (packetid == (int)enPacketIDs.WrenchSwap)
            {
                WrenchSwap();
            }
        }
        public enum enPacketIDs
        {
            ClearFilter = 99991001,
            SetFilter = 99991002,
            WrenchSwap = 99991003,
            ShowItemToggle = 99991004
        }

        public virtual void WrenchSwap()
        {
            if (Api is ICoreClientAPI)
            {
                (Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, (int)enPacketIDs.WrenchSwap, null);
                return;
            }
            switchOn = !switchOn;
            MarkDirty();
        }
        
        #endregion
        public void OnServerTick(float dt)
        {
            if (!IsOn|| fuel <= 0) { return; }
            DoFurnaceChecks();
            BlockPos usepos = Pos.Copy().Offset(BlockFacing.UP);
            BlockCoalPile pile=Api.World.BlockAccessor.GetBlock(usepos) as BlockCoalPile;
            if (pile == null) {
                Block piletype = Api.World.BlockAccessor.GetBlock(new AssetLocation("game:coalpile"));
                Api.World.BlockAccessor.SetBlock(piletype.Id, usepos);
                return;
            }
            BlockEntityCoalPile becp = Api.World.BlockAccessor.GetBlockEntity(usepos) as BlockEntityCoalPile;
            if (becp == null) { return; }
            if (becp.inventory == null) { return; }
            if (becp.inventory[0].Itemstack == null || becp.inventory[0].StackSize == 0)
            {
                ItemStack newstack = new ItemStack(fuelitems[0], 1);
                fuel--;
                becp.inventory[0].Itemstack = newstack;
                becp.MarkDirty();
                MarkDirty();
                return;
            }
            ItemStack stack = becp.inventory[0].Itemstack;
            if (stack.Item == null || stack.StackSize < 8)
            {
                stack.StackSize += 1;
                fuel--;
                becp.MarkDirty();
            }
            if (ShouldIgnite&& !becp.IsBurning&& becp.CanIgnite)
            {
                MethodInfo tryignite = becp.GetMethod("TryIgnite");
                tryignite.Invoke(becp,null);
            }
            
            MarkDirty();
        }
        void DoFurnaceChecks()
        {
            coffinReady = false;
            BlockPos checkpoint = new BlockPos(Pos.X, Pos.Y + 3, Pos.Z);
            coffin = Api.World.BlockAccessor.GetBlockEntity(checkpoint) as BlockEntityStoneCoffin;
            if (coffin == null) { coffinReady = false; return; }
            if (coffin.IsFull) { coffinReady = true; }
        }


        #region savingandinfo
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("fuel", fuel);
            tree.SetBool("switchOn", switchOn);
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            fuel = tree.GetInt("fuel");
            switchOn = tree.GetBool("switchOn", true);
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if (IsOn) { dsc.AppendLine("Will load coal stack (right click with wrench to turn off"); }
            else { dsc.AppendLine("Coal Pile loading is off (right click with wrench to turn on"); }
            dsc.AppendLine("Storing " + fuel + " fuel");
        }
        #endregion
    }
}
