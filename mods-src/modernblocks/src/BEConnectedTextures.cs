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

namespace modernblocks.src
{
    class BEConnectedTextures : BlockEntity, ITexPositionSource
    {
        string[] suffix => new string[] { "0", "n", "e", "ne", "s", "ns", "es", "nes", "w", "wn", "ew", "wne", "sw", "swn", "esw", "nesw" };
        const int n_ = 1;
        const int e_ = 2;
        const int s_ = 4;
        const int w_ = 8;
        TextureAtlasPosition texPosition;
        ITexPositionSource texsource;
        public TextureAtlasPosition this[string textureCode]
        {
            get
            {
                return texsource[textureCode];
            }
        }

        public Size2i AtlasSize => (Api as ICoreClientAPI).BlockTextureAtlas.Size;
        ICoreClientAPI capi;
        //string basetexturename = "block/stone/concretetile/concretetile-";

        //string fulltexturename => basetexturename + suffix[directionmap];

        MeshRef meshref;
        MeshData meshdata;
        Block gettextureblock;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            gettextureblock = api.World.BlockAccessor.GetBlock(new AssetLocation("modernblocks:concretelargetile"));
            if (api is ICoreClientAPI)
            {
                capi = api as ICoreClientAPI;
                texsource = capi.Tesselator.GetTexSource(gettextureblock);
                GenMesh(false);

            }

        }




        public virtual void GenMesh(bool triggerneighbors)
        {
            if (capi == null) { return; }
            meshref?.Dispose();
            meshref = null;
            meshdata = new MeshData();
            Shape shape = capi.TesselatorManager.GetCachedShape(new AssetLocation("modernblocks:block/tiledconcrete"));
            ShapeElement shapeelement = shape.Elements[0];
            Dictionary<BlockFacing, bool> neighbors = new Dictionary<BlockFacing, bool>();
            foreach (BlockFacing bf in BlockFacing.ALLFACES)
            {
                //BEMultiBlockTexture bembt = capi.World.BlockAccessor.GetBlockEntity(Pos.Copy().Offset(bf)) as BEMultiBlockTexture;
                Block otherblock = capi.World.BlockAccessor.GetBlock(Pos.Copy().Offset(bf));
                if (otherblock != Block) { neighbors[bf] = false; }
                else { neighbors[bf] = true; }
            }
            foreach (String facename in shapeelement.Faces.Keys)
            {
                if (facename == "up")
                {
                    int suffindex = 0;
                    if (!neighbors[BlockFacing.NORTH]) { suffindex += 1; }
                    if (!neighbors[BlockFacing.EAST]) { suffindex += 2; }
                    if (!neighbors[BlockFacing.SOUTH]) { suffindex += 4; }
                    if (!neighbors[BlockFacing.WEST]) { suffindex += 8; }
                    shapeelement.Faces[facename].Texture = "#" + suffix[suffindex];
                }
                else if (facename == "down")
                {
                    int suffindex = 0;
                    if (!neighbors[BlockFacing.SOUTH]) { suffindex += 1; }
                    if (!neighbors[BlockFacing.EAST]) { suffindex += 2; }
                    if (!neighbors[BlockFacing.NORTH]) { suffindex += 4; }
                    if (!neighbors[BlockFacing.WEST]) { suffindex += 8; }
                    shapeelement.Faces[facename].Texture = "#" + suffix[suffindex];
                }
                else if (facename == "east")
                {
                    int suffindex = 0;
                    if (!neighbors[BlockFacing.UP]) { suffindex += 1; }
                    if (!neighbors[BlockFacing.NORTH]) { suffindex += 2; }
                    if (!neighbors[BlockFacing.DOWN]) { suffindex += 4; }
                    if (!neighbors[BlockFacing.SOUTH]) { suffindex += 8; }
                    shapeelement.Faces[facename].Texture = "#" + suffix[suffindex];
                }
                else if (facename == "west")
                {
                    int suffindex = 0;
                    if (!neighbors[BlockFacing.UP]) { suffindex += 1; }
                    if (!neighbors[BlockFacing.SOUTH]) { suffindex += 2; }
                    if (!neighbors[BlockFacing.DOWN]) { suffindex += 4; }
                    if (!neighbors[BlockFacing.NORTH]) { suffindex += 8; }
                    shapeelement.Faces[facename].Texture = "#" + suffix[suffindex];
                }
                else if (facename == "south")
                {
                    int suffindex = 0;
                    if (!neighbors[BlockFacing.UP]) { suffindex += 1; }
                    if (!neighbors[BlockFacing.EAST]) { suffindex += 2; }
                    if (!neighbors[BlockFacing.DOWN]) { suffindex += 4; }
                    if (!neighbors[BlockFacing.WEST]) { suffindex += 8; }
                    shapeelement.Faces[facename].Texture = "#" + suffix[suffindex];
                }
                else if (facename == "north")
                {
                    int suffindex = 0;
                    if (!neighbors[BlockFacing.UP]) { suffindex += 1; }
                    if (!neighbors[BlockFacing.WEST]) { suffindex += 2; }
                    if (!neighbors[BlockFacing.DOWN]) { suffindex += 4; }
                    if (!neighbors[BlockFacing.EAST]) { suffindex += 8; }
                    shapeelement.Faces[facename].Texture = "#" + suffix[suffindex];
                }
                else
                {
                    int a = 1; //what did i miss?
                }

            }
            capi.Tesselator.TesselateShape("tileconcrete" + Pos.ToString(), shape, out meshdata, this);

            //MarkDirty(true);
        }



        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {

            mesher.AddMeshData(meshdata);

            meshref = capi.Render.UploadMesh(meshdata);
            return true;
        }


        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);

        }

        public override void OnBlockRemoved()
        {
            meshref?.Dispose();
            meshref = null;
            base.OnBlockRemoved();
        }
    }

    
}