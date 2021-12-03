using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace qptech.src
{
    class BEMPGenerator : BEElectric
    {
       

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            //this.distributionFaces.Add(BlockFacing.FromCode(Block.Variant["side"]).Opposite);
        }

       

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            base.OnTesselation(mesher, tessThreadTesselator);
            mesher.AddMeshData((Block as BlockMPGenerator)?.statorMesh);
            return true;
        }
    }
}
