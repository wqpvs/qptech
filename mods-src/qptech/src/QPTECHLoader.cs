using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using qptech.src.misc;
using qptech.src.multiblock;
using qptech.src.pipes;
using qptech.src.networks;
using qptech.src.itemtransport;
using qptech.src.Electricity;
using Vintagestory.API.Util;
using Vintagestory.API.Datastructures;
using ProtoBuf;


namespace qptech.src
{
    class QPTECHLoader:ModSystem
    {
        public static QPTechClientConfig clientconfig;
        public static QPTechServerConfig serverconfig;
        string clientconfigfile = "qptechclientconfig.json";
        string serverconfigfile = "qptechserverconfig.json";
        ICoreAPI api;
        ICoreClientAPI capi;
        ICoreServerAPI sapi;
        QPTECHServerData maindata;
        public int GlobalFluxStorage => maindata==null?0:maindata.storedpower;
        public static QPTECHLoader qptechmain;
        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            qptechmain = this;
            if (api is ICoreClientAPI )
            {
                capi = api as ICoreClientAPI;
                try
                {
                    clientconfig = api.LoadModConfig<QPTechClientConfig>(clientconfigfile);
                }
                catch
                {
                    

                }
                if (clientconfig == null)
                {
                    clientconfig = new QPTechClientConfig();
                    api.StoreModConfig<QPTechClientConfig>(clientconfig, clientconfigfile);
                }
            }
            else
            {
                sapi = api as ICoreServerAPI;
                try
                {
                    serverconfig = api.LoadModConfig<QPTechServerConfig>(serverconfigfile);
                }
                catch
                {


                }
                if (serverconfig == null)
                {
                    serverconfig = new QPTechServerConfig();
                    api.StoreModConfig<QPTechServerConfig>(serverconfig, serverconfigfile);
                }
            }
        }
        public override void Start(ICoreAPI api)
        {
            this.api = api;

            RegisterDefaultBlocks();
            RegisterDefaultBlockEntitys();
            RegisterDefaultItems();
            RegisterDefaultBehavior();
    
            if (api is ICoreClientAPI)
            {
                capi = api as ICoreClientAPI;
                capi.RegisterCommand("showitempipecontents", "If false Item Pipes won't render their contents (for performance reasons).", "", CmdShowItemPipe);
            }
            else
            {
                sapi = api as ICoreServerAPI;
                sapi.RegisterCommand("poweradd", "Add power to the global power storage", "", CmdAddAndShowPower);
            }
        }

        private void RegisterDefaultBlocks() 
        {


            #region BlockElectrical

            api.RegisterBlockClass("BlockEForge", typeof(BlockEForge));
            api.RegisterBlockClass("ElectricalBlock", typeof(ElectricalBlock));
            api.RegisterBlockClass("BlockClayformer", typeof(BlockClayformer));
            api.RegisterBlockClass("BlockMetalPress", typeof(BlockMetalPress));
            api.RegisterBlockClass("BlockWire", typeof(BlockWire));
            api.RegisterBlockClass("BlockJunction", typeof(BlockJunction));
            api.RegisterBlockClass("BlockPowerRod", typeof(BlockPowerRod));

            #endregion BlockElectrical
            #region Liquid

            api.RegisterBlockClass("BlockPump", typeof(BlockPump));
            api.RegisterBlockClass("BlockTank", typeof(BlockTank));
            api.RegisterBlockClass("BlockFluidPipe", typeof(BlockFluidPipe));

            #endregion Liquid

            api.RegisterBlockClass("BlockElectricGenerator", typeof(BlockMPGenerator));
            api.RegisterBlockClass("BlockElectricMotor", typeof(BlockElectricMotor));


            api.RegisterBlockClass("BlockItemTransport", typeof(BlockItemTransport));

            api.RegisterBlockClass("BlockDoorPart", typeof(BlockDoorPart));
            api.RegisterBlockClass("BlockTemporalPocket", typeof(BlockTemporalPocket));
            api.RegisterBlockClass("BlockCannedMeal", typeof(BlockCannedMeal));
            api.RegisterBlockClass("BlockDummy", typeof(BlockDummy));
        }

        private void RegisterDefaultBlockEntitys()
        {

            #region BEElectrical
            
            api.RegisterBlockEntityClass("BEEJunction", typeof(BEEJunction));
            api.RegisterBlockEntityClass("BEEForge", typeof(BEEForge));
            api.RegisterBlockEntityClass("BEEWire", typeof(BEEWire));
            api.RegisterBlockEntityClass("BEEAssembler", typeof(BEEAssembler));
            api.RegisterBlockEntityClass("BEEGenerator", typeof(BEEGenerator));
            api.RegisterBlockEntityClass("BEEMixer", typeof(BEEMixer));
            api.RegisterBlockEntityClass("BECoalPileStoker", typeof(BECoalPileStoker));
            api.RegisterBlockEntityClass("BEECrucible", typeof(BEECrucible));
            api.RegisterBlockEntityClass("BEEIndustrialGenerator", typeof(BEEIndustrialGenerator));
            api.RegisterBlockEntityClass("BEEPowerHatch", typeof(BEEPowerHatch));
            api.RegisterBlockEntityClass("BEEMBHeater", typeof(BEEMBHeater));
            api.RegisterBlockEntityClass("BEEMacerator", typeof(BEEMacerator));
            api.RegisterBlockEntityClass("BEEHVAC", typeof(BEEHVAC));
            api.RegisterBlockEntityClass("BEEKiln", typeof(BEEKiln));
            api.RegisterBlockEntityClass("BEELightBulb", typeof(BEELightBulb));
            api.RegisterBlockEntityClass("BEESolarPlane", typeof(BEESolarPlane));

            #endregion BEElectrical
            #region Item Pipes

            api.RegisterBlockEntityClass("ItemPipe", typeof(ItemPipe));
            api.RegisterBlockEntityClass("ItemSplitter", typeof(ItemSplitter));

            #endregion Item Pipes
            #region Liquid 

            api.RegisterBlockEntityClass("BEWaterTower", typeof(BEWaterTower));
            api.RegisterBlockEntityClass("BEFluidPipe", typeof(BEFluidPipe));
            api.RegisterBlockEntityClass("BEIrrigator", typeof(BEIrrigator));
            api.RegisterBlockEntityClass("BEFluidPump", typeof(BEFluidPump));
            api.RegisterBlockEntityClass("BEClearFluidTank", typeof(BEClearFluidTank));

            #endregion Liquid 
            #region Process

            api.RegisterBlockEntityClass("BEProcessToProcess", typeof(BEProcessToProcess));
            api.RegisterBlockEntityClass("BEProcessToProcessFluidUser", typeof(BEProcessToProcessFluidUser));
            api.RegisterBlockEntityClass("BEERecipeProcessor", typeof(BEERecipeProcessor));
            api.RegisterBlockEntityClass("BEEProcessingSupplier", typeof(BEEProcessingSupplier));

            #endregion  Process
            #region BEMP

            api.RegisterBlockEntityClass("ElectricGenerator", typeof(BEMPGenerator));
            api.RegisterBlockEntityClass("ElectricMotor", typeof(BEMPMotor));

            #endregion BEMP

            api.RegisterBlockEntityClass("BEPowerFlag", typeof(BEPowerFlag));
            api.RegisterBlockEntityClass("BETemporalPocket", typeof(BETemporalPocket));
            api.RegisterBlockEntityClass("MBItemHatch", typeof(MBItemHatch));
            api.RegisterBlockEntityClass("BESlidingDoorCore", typeof(BESlidingDoorCore));
            api.RegisterBlockEntityClass("BEReportsClicks", typeof(BEReportsClicks));
            api.RegisterBlockEntityClass("BEDummyBlock", typeof(BEDummyBlock));
        }

        private void RegisterDefaultItems()
        {
            api.RegisterItemClass("ItemMiningDrill", typeof(ItemMiningDrill));
            api.RegisterItemClass("TabletTool", typeof(TabletTool));
            api.RegisterItemClass("ItemQuarryTool", typeof(ItemQuarryTool));
            api.RegisterItemClass("ItemJetPack", typeof(ItemJetPack));
            api.RegisterItemClass("ItemWire", typeof(ItemWire));
            
        }

        private void RegisterDefaultBehavior() 
        {
            api.RegisterBlockEntityBehaviorClass("MPMotor", typeof(BEBMPMotor));
            api.RegisterBlockEntityBehaviorClass("MPGenerator", typeof(BEBMPGenerator));
            api.RegisterBlockEntityBehaviorClass("BEBMultiDummy", typeof(BEBMultiDummy));
            api.RegisterBlockClass("BlockDummy", typeof(BlockDummy));
            api.RegisterBlockEntityClass("BEEMixer", typeof(BEEMixer));
            api.RegisterBlockEntityClass("BECoalPileStoker", typeof(BECoalPileStoker));
            //api.RegisterBlockClass("BlockElectricMotor", typeof(BlockElectricMotor));
            //api.RegisterBlockEntityClass("BEEMotor", typeof(BEEMotor));
        }
        
        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;

            api.Event.SaveGameLoaded += OnSaveGameLoading;
            api.Event.GameWorldSave += OnSaveGameSaving;

            
        }

        const string maindataname = "maindata";

        private void OnSaveGameLoading()
        {
            byte[] data = sapi.WorldManager.SaveGame.GetData(maindataname);

            maindata = data == null ? new QPTECHServerData() : SerializerUtil.Deserialize<QPTECHServerData>(data);
        }

        private void OnSaveGameSaving()
        {
            sapi.WorldManager.SaveGame.StoreData(maindataname, SerializerUtil.Serialize(maindata));
        }

        private void CmdShowItemPipe(int groupId, CmdArgs args)
        {
            if (capi == null) { return; }
            if (args == null ||args.Length==0 ) { capi.ShowChatMessage("Item Pipes show items currently set to " + clientconfig.showPipeItems); return; }
            else if (args[0] == "true" || args[0] == "1" || args[0] == "on") { clientconfig.showPipeItems = true; }
            else if (args[0] == "false" || args[0] == "0" || args[0] == "off") { clientconfig.showPipeItems = false; }
            
            capi.StoreModConfig<QPTechClientConfig>(clientconfig, clientconfigfile);
            capi.ShowChatMessage("Item Pipes show items has been set to "+clientconfig.showPipeItems);
        }

        private void CmdAddAndShowPower(IPlayer player, int groupId, CmdArgs args)
        {
            if (sapi == null) { return; }
            if (maindata == null) { sapi.BroadcastMessageToAllGroups("No maindata exists",EnumChatType.Notification);return; }
            maindata.storedpower++;
            sapi.BroadcastMessageToAllGroups("Stored Power is now " + maindata.storedpower + " temporal flux", EnumChatType.Notification);
        }


    }
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class QPTECHServerData
    {
        public int storedpower = 0;
    }
}
