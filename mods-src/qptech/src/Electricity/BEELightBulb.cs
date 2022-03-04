using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace qptech.src
{
    class BEELightBulb : BEElectric
    {

        // public byte[] lightOn = { 7, 7, 16 };

        public override void Initialize(ICoreAPI api)
        {
 
            base.Initialize(api);

            //this.Block.LightHsv.SetValue(lightOn[2], 2);

            if (Block.Attributes.Exists == true)
            {
                usePower = Block.Attributes["useFlux"].AsInt(usePower);
            }
     
            if (Api.Side is EnumAppSide.Server) { RegisterGameTickListener(OnLightTick, 75); }

        }

        public virtual void OnLightTick(float tick)
        {
            if (Api.Side is EnumAppSide.Server)
            {
                if (IsPowered == true)
                {
                    setBlockState("on");
                    //Block.LightHsv[2] = lightOn[2];
                }
                else if (IsPowered == false)
                {
                    setBlockState("off");
                    //Api.World.BulkBlockAccessor.RemoveBlockLight(lightOn, Pos);
                }
            }
        }


        public void setBlockState(string state)
        {

            AssetLocation loc = Block.CodeWithVariant("powerstate", state);
            Block block = Api.World.GetBlock(loc);
            if (block == null) return;

            Api.World.BlockAccessor.MarkBlockDirty(Pos);
            Api.World.BlockAccessor.ExchangeBlock(block.Id, Pos);
            this.Block = block;

        }

        //public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        //{

        //    mesher.AddMeshData(getOrCreateMesh("powerstate"));

        //    return true;

        //}

        public MeshData getOrCreateMesh(string powerstate)
        {
            Dictionary<string, MeshData> Meshes = ObjectCacheUtil.GetOrCreate(Api, "lightbulk-meshes", () => new Dictionary<string, MeshData>());

            string key = powerstate;

            MeshData meshdata;
            if (!Meshes.TryGetValue(key, out meshdata))
            {
                Block block = Api.World.BlockAccessor.GetBlock(Pos);
                if (block.BlockId == 0) return null;

                MeshData[] meshes = new MeshData[17];
                ITesselatorAPI mesher = ((ICoreClientAPI)Api).Tesselator;

                mesher.TesselateBlock(block, out meshdata);
            }

            return meshdata;
        }

        public override void OnBlockRemoved()
        {
            Api.World.BlockAccessor.RemoveBlockLight(Block.LightHsv, Pos);
            base.OnBlockRemoved();
        }
    }
}