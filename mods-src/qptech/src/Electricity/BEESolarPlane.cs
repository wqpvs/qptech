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
    class BEESolarPlane : BEElectric
    {

        protected BlockPos upPos;

        WeatherSystemBase wsys;

        MeshData meshdata;

        protected float sunLightStrength;

        protected int timeOfDay;

        public bool light = false;

        public int fluxBonus { get; set; }

        public int[] timeofday => new int[timeOfDay];

        RoomRegistry roomreg;
        public int roomness;


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

            if (Block.Attributes.Exists == true)
            {
                fluxBonus = Block.Attributes["fluxBonus"].AsInt(fluxBonus);
            }

            if (api.Side is EnumAppSide.Server) 
            { 
                RegisterGameTickListener(OnSunTick, 100);
            }
        }

        public void OnSunTick(float tick)
        {
            sunLightStrength = Api.World.BlockAccessor.GetLightLevel(Pos, EnumLightLevelType.MaxTimeOfDayLight);
            if (Api.Side is EnumAppSide.Server)
            {
                if (IsOn)
                {
                    if (sunLightStrength < 8)
                    {
                        NightOrDay(false);
                    }
                    else
                    {
                        NightOrDay(true);
                    }
                }
            }
        }

        public bool NightOrDay(bool light)
        {
            timeOfDay = Api.World.Calendar.FullHourOfDay;

            if (isOn)
            {
                if (sunLightStrength > 9 & timeofday.Length < 10)
                {
            
                    setBlockState("morning");
                    genPower = 1 + fluxBonus;

                }
                else if (sunLightStrength > 9 & timeofday.Length <= 14)
                {
              
                    setBlockState("midday");
                    genPower = 2 + fluxBonus;
                }
                else if (sunLightStrength > 9 & timeofday.Length < 21)
                {
         
                    setBlockState("evening");
                    genPower = 1 + fluxBonus;
                }
                else
                {
 
                    setBlockState("night");
                    genPower = 0;
                }
            }
   
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

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {

            mesher.AddMeshData(getOrCreateMesh("timestate"));

            return true;

        }

        public MeshData getOrCreateMesh(string timestate)
        {
            Dictionary<string, MeshData> Meshes = ObjectCacheUtil.GetOrCreate(Api, "solarpanel-meshes", () => new Dictionary<string, MeshData>());

            string key = timestate;

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