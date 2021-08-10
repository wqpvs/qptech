using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using qptech.src.misc;



namespace qptech.src
{
    class ElectricityLoader:ModSystem
    {
        
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
           
            api.RegisterBlockEntityClass("BEEWire", typeof(BEEWire));
            api.RegisterBlockEntityClass("BEEAssembler", typeof(BEEAssembler));
            api.RegisterBlockEntityClass("BEEGenerator", typeof(BEEGenerator));
            api.RegisterBlockClass("ElectricalBlock", typeof(ElectricalBlock));
            api.RegisterBlockClass("BlockWire", typeof(BlockWire));
            api.RegisterBlockClass("BlockCannedMeal", typeof(BlockCannedMeal));
            api.RegisterItemClass("ItemQuarryTool", typeof(ItemQuarryTool));
            api.RegisterItemClass("ItemJetPack", typeof(ItemJetPack));
            api.RegisterBlockEntityClass("BEEForge", typeof(BEEForge));
            api.RegisterBlockClass("BlockEForge", typeof(BlockEForge));
            api.RegisterBlockEntityClass("BEPowerFlag", typeof(BEPowerFlag));
            api.RegisterBlockClass("BlockClayformer",typeof(BlockClayformer));
            api.RegisterBlockClass("BlockMetalPress",typeof(BlockMetalPress));
            api.RegisterBlockClass("BlockTemporalPocket", typeof(BlockTemporalPocket));
            api.RegisterBlockEntityClass("BETemporalPocket", typeof(BETemporalPocket));
            api.RegisterBlockEntityClass("BEEMacerator", typeof(BEEMacerator));
            api.RegisterBlockEntityClass("BEEBlastFurnace", typeof(BEEBlastFurnace));
            api.RegisterBlockEntityClass("BEEHVAC",typeof(BEEHVAC));
            api.RegisterBlockEntityClass("BEEKiln", typeof(BEEKiln));
            
            api.RegisterBlockEntityClass("ElectricMotor", typeof(BEMPMotor));
            api.RegisterBlockClass("BlockElectricMotor", typeof(BlockElectricMotor));
            api.RegisterBlockEntityBehaviorClass("MPMotor", typeof(BEBMPMotor));
            
            api.RegisterBlockEntityClass("ElectricGenerator", typeof(BEMPGenerator));
            api.RegisterBlockClass("BlockElectricGenerator", typeof(BlockMPGenerator));
            api.RegisterBlockEntityBehaviorClass("MPGenerator", typeof(BEBMPGenerator));

            api.RegisterBlockEntityClass("BEWaterTower", typeof(BEWaterTower));
            //api.RegisterBlockClass("BlockElectricMotor", typeof(BlockElectricMotor));
            //api.RegisterBlockEntityClass("BEEMotor", typeof(BEEMotor));

        }
    }
}
