using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using qptech.src.networks;

namespace Vintagestory.GameContent
{

    public class ModdedFirePit : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterBlockEntityClass("ModdedFirepit", typeof(BlockEntityModdedFirepit));
        }

        public class BlockEntityModdedFirepit : BlockEntityFirepit,IProcessingSupplier
        {
            public float heatMod = 1f;
            public bool Blockinit { get; set; } = false;

            public override void Initialize(ICoreAPI api)
            {
                base.Initialize(api);
                if (Block.Attributes != null)
                {
                    heatMod = Block.Attributes["heatModifier"].AsFloat(1f);
                    Blockinit = true;
                }
            }
            public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
            {

                return false;
                /*if (Block == null || Block.Code.Path.Contains("construct")) return false;


                ItemStack contentStack = inputStack == null ? outputStack : inputStack;
                MeshData contentmesh = getContentMesh(contentStack, tesselator);
                if (contentmesh != null)
                {
                    mesher.AddMeshData(contentmesh);
                }

                string burnState = Block.Variant["burnstate"];
                string contentState = CurrentModel.ToString().ToLowerInvariant();
                if (burnState == "cold" && fuelSlot.Empty) burnState = "extinct";

                mesher.AddMeshData(getOrCreateMesh(burnState, contentState));

                return true;*/
            }
            
            public override float HeatModifier
            {
                get { return heatMod; }
            }
            public bool RequestProcessing(string process, double amount)
            {
                if (process != "heating") { return false; }
                if (this.furnaceTemperature >= amount) { return true; }
                return false;
            }
            public double RequestProcessing(string process)
            {
                if (process != "heating") { return 0; }
                return this.furnaceTemperature;
            }
            public bool CheckProcessing(string process)
            {
                return process == "heating";
            }
        }
    }
}