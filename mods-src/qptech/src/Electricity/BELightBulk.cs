using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace qptech.src
{
    class BELightBulk : BEElectric
    {

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
        }

        public void setBlockState(string state)
        {
            AssetLocation loc = Block.CodeWithVariant("powerstate", state);
            Block block = Api.World.GetBlock(loc);
            if (block == null) return;

            Api.World.BlockAccessor.ExchangeBlock(block.Id, Pos);
            this.Block = block;

        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {

            if (IsPowered)
            {
                setBlockState("on");
            }
            else if (!IsPowered)
            {
                setBlockState("off");
            }

            mesher.AddMeshData(getOrCreateMesh("powerstate"));

            return true;

        }

        public MeshData getOrCreateMesh(string powerstate)
        {
            Dictionary<string, MeshData> Meshes = ObjectCacheUtil.GetOrCreate(Api, "lightbulk-meshes", () => new Dictionary<string, MeshData>());

            string key = powerstate;

            MeshData meshdata;
            if (!Meshes.TryGetValue(key, out meshdata))
            {
                Block block = Api.World.BlockAccessor.GetBlock(Pos);
                if (block.BlockId == 0) return null;

                MeshData[] meshes = new MeshData[19];
                ITesselatorAPI mesher = ((ICoreClientAPI)Api).Tesselator;

                mesher.TesselateBlock(block, out meshdata);
            }

            return meshdata;
        }

    }
}