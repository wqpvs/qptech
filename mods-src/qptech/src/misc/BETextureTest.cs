using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.API.Util;


namespace qptech.src
{
    public class BETextureTest : BlockEntity,ITexPositionSource
    {
        List<string> gaugetextures;
        int texno = 0;
        float pcttracker = 0.5f;
        public TextureAtlasPosition this[string textureCode]
        {
            get
            {
                //capi.BlockTextureAtlas.Positions[Block.Textures[path].Baked.TextureSubId];
                return capi.BlockTextureAtlas.Positions[atlasBlock.Textures[gaugetextures[texno]].Baked.TextureSubId];
            }
        }
        public Size2i AtlasSize => (Api as ICoreClientAPI).BlockTextureAtlas.Size;
        ICoreClientAPI capi;
        Block atlasBlock;
       
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api is ICoreClientAPI)
            {
                capi = api as ICoreClientAPI;
            }
            gaugetextures = new List<string>();
            gaugetextures.Add("roundgauge-0");
            gaugetextures.Add("roundgauge-10");
            gaugetextures.Add("roundgauge-20");
            gaugetextures.Add("roundgauge-30");
            gaugetextures.Add("roundgauge-40");
            gaugetextures.Add("roundgauge-50");
            gaugetextures.Add("roundgauge-60");
            gaugetextures.Add("roundgauge-70");
            gaugetextures.Add("roundgauge-80");
            gaugetextures.Add("roundgauge-90");
            gaugetextures.Add("roundgauge-100");
            RegisterGameTickListener(OnTick, 100);
            atlasBlock = api.World.GetBlock(new AssetLocation("machines:dummygauge"));

        }
        float rot = 0;
        public void OnTick(float dt)
        {

            pcttracker = 0;

            BlockPos bp = Pos.Copy().Offset(BlockFacing.DOWN);

            BlockEntity checkBE = Api.World.BlockAccessor.GetBlockEntity(bp);
            var bee = checkBE as BEElectric;
            if (bee != null)
            {
                
                switch (bee.Block.LastCodePart())
                {
                    case "east": rot = 270;break;
                    case "south": rot = 180;break;
                    case "west": rot = 90;break;
                    case "north": rot = 0;break;
                }
                if (bee.IsOn)
                {
                    pcttracker = bee.CapacitorPercentage;
                    if (pcttracker > 1) { pcttracker = 1; }
                    if (pcttracker < 0) { pcttracker = 0; }
                }
            }

            int newtexno = (int)((float)(gaugetextures.Count-1) * pcttracker);
            
            if (newtexno != texno)
            {
                texno = newtexno;
                this.MarkDirty(true);
            }
           
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            dsc.Append(pcttracker.ToString()+" "+texno.ToString());

            //dsc.AppendLine("IN:" + inputConnections.Count.ToString() + " OUT:" + outputConnections.Count.ToString());
        }
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            
            Shape gaugeshape = capi.TesselatorManager.GetCachedShape(new AssetLocation("machines:block/metal/electric/roundgauge0"));
            
            
            MeshData meshdata;
            capi.Tesselator.TesselateShape("roundgauge0"+Pos.ToString(), gaugeshape, out meshdata, this);
            //TextureAtlasPosition tap = capi.BlockTextureAtlas.Positions[atlasBlock.Textures[gaugetextures[texno]].Baked.TextureSubId];
           // meshdata = meshdata.WithTexPos(tap);
            //5,12,13 in model program units = what in actual coordinates?
            float translatefactor = 16;
            meshdata.Translate(new Vec3f(4/translatefactor, 10/translatefactor, 3/translatefactor));
            
            meshdata.Rotate(new Vec3f(0,0,0), 0, GameMath.DEG2RAD*rot, 0);
            
            mesher.AddMeshData(meshdata);
            return base.OnTesselation(mesher,tessThreadTesselator);
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);


            //if (type == null) type = defaultType; // No idea why. Somewhere something has no type. Probably some worldgen ruins
            //texno = tree.GetInt("texno");
            
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            //tree.SetInt("texno", texno);
            
        }
    }

    
    public class TextureTestLoader : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterBlockEntityClass("BETextureTest", typeof(BETextureTest));
        }
    }
}
