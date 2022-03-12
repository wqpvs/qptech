using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace qptech.src
{
    class BEETEST : BEElectric
    {

        protected BlockPos upPos;

        WeatherSystemBase wsys;

        protected float sunLightStrength;

        protected int timeOfDay;

        public bool light = false;

        public new virtual int AvailablePower()
        {
            if (!isOn || sunLightStrength == 0) { return 0; }
            return genPower;
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            wsys = api.ModLoader.GetModSystem<WeatherSystemBase>();
            upPos = base.Pos.UpCopy();

            setBlockState("morning");

            if (api.Side is EnumAppSide.Server) 
            { 
                RegisterGameTickListener(OnSunTick, 100);
            }
        }

        public void OnSunTick(float tick)
        {
            float dt = tick;
            if (Api.Side is EnumAppSide.Server)
            {
                if (IsOn)
                {
                    timeOfDay = Api.World.Calendar.FullHourOfDay;
                    var time = Api.World.BlockAccessor.GetLightLevel(upPos, EnumLightLevelType.MaxTimeOfDayLight);
                    sunLightStrength = Api.World.Calendar.GetDayLightStrength(Pos.X, Pos.Z);
                    if (sunLightStrength < 1f)
                    {
                        dt = timeOfDay * 1000;
                        NightOrDay(false);
                    }
                    else
                    {
                        NightOrDay(true);
                    }
                    //NightOrDay(light);
                }
            }
        }

        public bool NightOrDay(bool light)
        {
            if (timeOfDay >= 8 & timeOfDay <= 10) 
            {
                setBlockState("morning");
                genPower = 1; 
            }
            else if (timeOfDay >= 10 & timeOfDay <= 15) 
            {
                setBlockState("midday");
                genPower = 2; 
            }
            else if (timeOfDay >= 15 & timeOfDay <= 18) 
            {
                setBlockState("evening");
                genPower = 1; 
            }
            else {
                setBlockState("night");
                genPower = 0; 
            }

            //    int i = timeOfDay;
            //    for (i = timeOfDay; i <= timeOfDay; i++)
            //    {
            //        if (i <= 8)
            //        {
            //            genPower = 0;

            //        }
            //        else if (i > 8 & i <= 17) { genPower = 1; }
            //        else if (i > 18) { genPower = 0; }
            //}

            //if (timeOfDay <= 7)
            //{
            //    genPower = 0;
            //}
            //else if (timeOfDay > 7 / 8 & timeOfDay <= 12)
            //{
            //    genPower = (int)(sunLight * 50 / timeOfDay);
            //}
            //else if (timeOfDay > 12 & timeOfDay <= 18) { genPower = (int)(sunLight * 50 / -timeOfDay); }
            //else { genPower = 0; }
            //if (timeOfDay < 20) { genPower = 0; }
            return false;
        }

        public void setBlockState(string state)
        {

            AssetLocation loc = Block.CodeWithVariant("timestate", state);
            Block block = Api.World.GetBlock(loc);
            if (block == null) return;

            Api.World.BlockAccessor.ExchangeBlock(block.Id, Pos);
            this.Block = block;

        }

        //public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        //{

        //    mesher.AddMeshData(getOrCreateMesh("powerstate"));

        //    return true;

        //}

        public MeshData getOrCreateMesh(string timestate)
        {
            Dictionary<string, MeshData> Meshes = ObjectCacheUtil.GetOrCreate(Api, "solarpanel-meshes", () => new Dictionary<string, MeshData>());

            string key = timestate;

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

    }
}