using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;
using System.Text;

namespace qptech.src
{
    class BEBMPGenerator : BEBehaviorMPBase
    {
        BEMPGenerator generator { get => Blockentity as BEMPGenerator; }

        public float SpeedToFlux { get; set; } = 30f;
        public float OwnResistance { get; set; } = 0.003f;
        public float MinimumSpeed { get; set; } = 0.3f;
        public int generated = 0;

        public BEBMPGenerator(BlockEntity blockentity) : base(blockentity)
        {
            Blockentity = blockentity;

            OutFacingForNetworkDiscovery = BlockFacingExt.FromIndex((Block as BlockMPGenerator).powerInIndex);
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);

            if (Block?.Attributes != null)
            {
                SpeedToFlux = Block.Attributes["speedToFlux"].AsFloat(SpeedToFlux);
                OwnResistance = Block.Attributes["ownResistance"].AsFloat(OwnResistance);
            }

            switch (BlockFacing.FromCode(Block.Variant["side"]).Index)
            {
                case 0:
                    AxisSign = new int[] { -1, 0, 0 };
                    break;
                case 1:
                    AxisSign = new int[] { 0, 0, -1 };
                    break;
                case 2:
                    AxisSign = new int[] { -1, 0, 0 };
                    break;
                case 3:
                    AxisSign = new int[] { 0, 0, -1 };
                    break;
                default:
                    break;
            }

            Blockentity.RegisterGameTickListener(UpdateTF, 75);
            Blockentity.RegisterGameTickListener(UpdateMP, 1000);
        }

        public void UpdateTF(float dt)
        {
            /*switch (generator.DeviceState)
            {
                case BEEBaseDevice.enDeviceState.WARMUP:
                case BEEBaseDevice.enDeviceState.IDLE:
                case BEEBaseDevice.enDeviceState.MATERIALHOLD:
                case BEEBaseDevice.enDeviceState.ERROR:
                    break;
                case BEEBaseDevice.enDeviceState.RUNNING:
                    break;
                default:
                    break;
            }*/

            if (network == null) { return; }
            network.NetworkResistance += OwnResistance;
            if (network.Speed < MinimumSpeed) { generated = 0; return; }
            generated = (int)(network.Speed * SpeedToFlux);
            generator.SetAvailablePower(true);
            // **** THIS NEEDS TO BE REDONE!
            
        }

        public void UpdateMP(float dt)
        {
            network?.updateNetwork(manager.getTickNumber());
        }

        public override float GetResistance() => OwnResistance;
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            dsc.AppendLine("Generation :" + generated.ToString());
            if (network != null)
            {
                dsc.AppendLine("network speed:  " + Network.Speed.ToString());
                dsc.AppendLine("network resist: " + Network.NetworkResistance.ToString());
            }
        }
    }
}
