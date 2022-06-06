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


namespace qptech.src
{
    class BETemporalPocket:BlockEntityGenericContainer
    {
        string accessing = "";
        public bool Busy => (accessing!="");
        static List<string> openinventories;
        
        public static List<string> OpenInventories
        {
            get
            {
                if (openinventories == null) { openinventories = new List<string>(); }
                return openinventories;
            }
        }
        public override string InventoryClassName => "Temporal Pocket";
        protected override void OnInvOpened(IPlayer player)
        {
            //if (simpleinventory.openinventories == null) { simpleinventory.openinventories = new List<string>(); }
           // if (simpleinventory.openinventories.Contains(player.PlayerUID)) { return; }
           if (OpenInventories.Contains(player.PlayerUID)) { return; }
            OpenInventories.Add(player.PlayerUID);
            if (accessing != "") { return; }
            accessing = player.PlayerUID;
            //simpleinventory.openinventories.Add(player.PlayerUID);
            if (Api is ICoreClientAPI) { return; }
            TryLoadInventory(player);
            
            base.OnInvOpened(player);
        }

        protected override void OnInvClosed(IPlayer player)
        {

            base.OnInvClosed(player);
            accessing = "";
            OpenInventories.Remove(player.PlayerUID);
            if (Api is ICoreClientAPI) { return; }
            TrySaveInventory(player);
            //simpleinventory.openinventories.Remove(player.PlayerUID);
            
            this.MarkDirty();
            
            
        }
        public virtual string GetChestFilename(IPlayer player)
        {
            return player.PlayerUID + "tchest.json";
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
    }

    
}
