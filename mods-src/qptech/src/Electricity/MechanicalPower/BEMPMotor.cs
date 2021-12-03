using Vintagestory.API.Client;

namespace qptech.src
{
    class BEMPMotor : BEEBaseDevice
    {



        protected override void DoDeviceProcessing()
        {
            if (!IsPowered)
            {
                deviceState = enDeviceState.IDLE;
            }
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            base.OnTesselation(mesher, tessThreadTesselator);
            mesher.AddMeshData((Block as BlockElectricMotor)?.statorMesh);
            return true;
        }
    }
}
