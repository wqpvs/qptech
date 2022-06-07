using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
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
using qptech.src.extensions;
using System.Text.RegularExpressions;

namespace qptech.src
{
    class BETemporalPocket:BlockEntityOpenableContainer
    {
        string accessing = "";
        public bool Busy => (accessing!="");
        static List<string> openinventories;
        public int quantitySlots = 64;
        public int quantityColumns = 8;
        public string inventoryClassName = "temporalpocket";
        public string dialogTitleLangCode = "Temporal Pocket";
        public bool retrieveOnly = false;

        public static List<string> OpenInventories
        {
            get
            {
                if (openinventories == null) { openinventories = new List<string>(); }
                return openinventories;
            }
        }
        public override string InventoryClassName => "Temporal Pocket";
        internal InventoryGeneric inventory;

        public override InventoryBase Inventory
        {
            get { return inventory; }
        }

        public override AssetLocation OpenSound { get; set; } = new AssetLocation("machines:sounds/block/hmmm");
        public override AssetLocation CloseSound { get; set; } = new AssetLocation("machines:sounds/block/hmmm");

        public override void Initialize(ICoreAPI api)
        {
            // Newly placed 
            if (inventory == null)
            {
                InitInventory(Block);
            }

            base.Initialize(api);
        }
        
        
        private void InitInventory(Block Block)
        {
            if (Block?.Attributes != null)
            {
                inventoryClassName = Block.Attributes["inventoryClassName"].AsString(inventoryClassName);
                dialogTitleLangCode = Block.Attributes["dialogTitleLangCode"].AsString(dialogTitleLangCode);
                quantitySlots = Block.Attributes["quantitySlots"].AsInt(quantitySlots);
                retrieveOnly = Block.Attributes["retrieveOnly"].AsBool(false);
            }

            inventory = new InventoryGeneric(quantitySlots, null, null, null);

            if (Block.Attributes?["spoilSpeedMulByFoodCat"].Exists == true)
            {
                inventory.PerishableFactorByFoodCategory = Block.Attributes["spoilSpeedMulByFoodCat"].AsObject<Dictionary<EnumFoodCategory, float>>();
            }

            if (Block.Attributes?["transitionSpeedMulByType"].Exists == true)
            {
                inventory.TransitionableSpeedMulByType = Block.Attributes["transitionSpeedMulByType"].AsObject<Dictionary<EnumTransitionType, float>>();
            }

            inventory.OnInventoryClosed += OnInvClosed;
            inventory.OnInventoryOpened += OnInvOpened;
            inventory.SlotModified += OnSlotModifid;
        }
        protected  void OnInvOpened(IPlayer player)
        {
            //if (simpleinventory.openinventories == null) { simpleinventory.openinventories = new List<string>(); }
            // if (simpleinventory.openinventories.Contains(player.PlayerUID)) { return; }
            inventory.PutLocked = retrieveOnly && player.WorldData.CurrentGameMode != EnumGameMode.Creative;
            if (OpenInventories.Contains(player.PlayerUID)) { return; }
            OpenInventories.Add(player.PlayerUID);
            if (accessing != "") { return; }
            accessing = player.PlayerUID;
            //simpleinventory.openinventories.Add(player.PlayerUID);
            if (Api is ICoreClientAPI) { return; }
            TryLoadInventory(player);
            
            
        }

        protected  void OnInvClosed(IPlayer player)
        {
            inventory.PutLocked = retrieveOnly;
            invDialog?.Dispose();
            invDialog = null;

            accessing = "";
            OpenInventories.Remove(player.PlayerUID);
            if (Api is ICoreClientAPI) { return; }
            TrySaveInventory(player);
            //simpleinventory.openinventories.Remove(player.PlayerUID);
            
            this.MarkDirty();
            
            
        }
        private void OnSlotModifid(int slot)
        {
            Api.World.BlockAccessor.GetChunkAtBlockPos(Pos)?.MarkModified();
        }
        public virtual string GetChestFilename(IPlayer player)
        {
            return RemoveSpecialCharacters(player.PlayerUID) + "tchest.json";
        }
        public static string RemoveSpecialCharacters(string str)
        {
            return Regex.Replace(str, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
        }
        void TryLoadInventory(IPlayer player)
        {
            this.Inventory.DiscardAll();

            try
            {
                byte[] data= ApiExtensions.LoadOrCreateDataFile<List<byte>>(Api, GetChestFilename(player)).ToArray();
                TreeAttribute loadtree = TreeAttribute.CreateFromBytes(data);
                

                if (loadtree != null) {
                    ItemSlot[]slots=Inventory.SlotsFromTreeAttributes(loadtree);
                    int c = 0;
                    foreach (ItemSlot slot in slots)
                    {
                        if (!slot.Empty)
                        {
                            Inventory[c] = slot;
                        }
                        c++;
                        if (c == Inventory.Count) { break; }
                    }
                    Inventory.ResolveBlocksOrItems();

                }
            }
            catch
            {
                int oops = 1;
            }
            
            this.MarkDirty();
        }
        public void OnBlockBroken()
        {
            Cleanup();
            base.OnBlockBroken();
        }
        void Cleanup()
        {
            
            //this.Inventory.DiscardAll();
            
        }
        public override void OnBlockRemoved()
        {
            Cleanup();
            base.OnBlockRemoved();
        }
        public override void OnBlockUnloaded()
        {
            Cleanup();
            base.OnBlockUnloaded();
        }
        void TrySaveInventory(IPlayer player)
        {
            if (Api is ICoreClientAPI) { return; }
            TreeAttribute newtree=new TreeAttribute();
            
            Inventory.SlotsToTreeAttributes(Inventory.ToArray<ItemSlot>(),newtree);

            //newtree will have correctly have the inventory at this point
            byte[] data = newtree.ToBytes();
            List<byte> datalist = data.ToList<byte>();
            ApiExtensions.SaveDataFile<List<byte>>(Api, GetChestFilename(player), datalist);

            
            this.Inventory.DiscardAll();
        }

        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (Api.World is IServerWorldAccessor)
            {
                byte[] data;

                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);
                    writer.Write("BlockEntityInventory");
                    writer.Write(Lang.Get(dialogTitleLangCode));
                    writer.Write((byte)8);
                    TreeAttribute tree = new TreeAttribute();
                    inventory.ToTreeAttributes(tree);
                    tree.ToBytes(writer);
                    data = ms.ToArray();
                }

                ((ICoreServerAPI)Api).Network.SendBlockEntityPacket(
                    (IServerPlayer)byPlayer,
                    Pos.X, Pos.Y, Pos.Z,
                    (int)EnumBlockContainerPacketId.OpenInventory,
                    data
                );

                byPlayer.InventoryManager.OpenInventory(inventory);
            }

            return true;
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            
            if (Block != null) tree.SetInt("forBlockId", Block.BlockId);
            if (Block != null) tree.SetString("accessing", accessing);
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            if (inventory == null)
            {
                if (tree.HasAttribute("forBlockId"))
                {
                    InitInventory(worldForResolving.GetBlock((ushort)tree.GetInt("forBlockId")));
                }
                else
                {
                    ITreeAttribute inventroytree = tree.GetTreeAttribute("inventory");
                    

                    InitInventory(null);
                }
            }
            accessing = tree.GetString("accessing", "");

            base.FromTreeAttributes(tree, worldForResolving);
        }
    }


}
