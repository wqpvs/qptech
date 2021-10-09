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
        protected override void OnInvOpened(IPlayer player)
        {
            //if (simpleinventory.openinventories == null) { simpleinventory.openinventories = new List<string>(); }
           // if (simpleinventory.openinventories.Contains(player.PlayerUID)) { return; }
            //if (accessing != "") { return; }
            //accessing = player.PlayerUID;
            //simpleinventory.openinventories.Add(player.PlayerUID);
            if (Api is ICoreClientAPI) { return; }
            TryLoadInventory(player);
            base.OnInvOpened(player);
        }

        protected override void OnInvClosed(IPlayer player)
        {
            
            TrySaveInventory(player);
            //simpleinventory.openinventories.Remove(player.PlayerUID);
            accessing = "";
            this.MarkDirty();
            
            base.OnInvClosed(player);
        }
        
        void TryLoadInventory(IPlayer player)
        {
            this.Inventory.DiscardAll();

            try
            {
                TreeAttribute loadtree = ApiExtensions.LoadOrCreateDataFile<TreeAttribute>(Api, "helloworld.json");
                if (loadtree != null) { Inventory.SlotsFromTreeAttributes(loadtree); }
            }
            catch
            {

            }
            
            this.MarkDirty();
        }
        public override void OnBlockBroken()
        {
            Cleanup();
            base.OnBlockBroken();
        }
        void Cleanup()
        {
            
            this.Inventory.DiscardAll();
            
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
            
            TreeAttribute newtree=new TreeAttribute();

            Inventory.SlotsToTreeAttributes(Inventory.ToArray<ItemSlot>(),newtree);

            ApiExtensions.SaveDataFile<TreeAttribute>(Api, "helloworld.json", newtree);
            this.Inventory.DiscardAll();
        }
    }

    
}
