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
        Dictionary<BlockFacing, bool> oldneighbors;
        TestRenderer testRenderer;
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
            if (api is ICoreClientAPI )
            {

                capi = api as ICoreClientAPI;
                capi.Event.RegisterRenderer(testRenderer = new TestRenderer(Pos, capi), EnumRenderStage.Opaque, "test");
                testRenderer.TextureName = new AssetLocation("modernblocks:block/connectedtextures/concretetile-nesw.png");
                testRenderer.GenModel();
            }
        }




        public virtual void GenMesh(bool triggerneighbors)
        {
            if (capi == null) { return; }
            
            Shape shape = capi.TesselatorManager.GetCachedShape(new AssetLocation("modernblocks:block/tiledconcrete"));
            ShapeElement shapeelement = shape.Elements[0];
            
            Dictionary<BlockFacing, bool> neighbors = new Dictionary<BlockFacing, bool>();
            foreach (BlockFacing bf in BlockFacing.ALLFACES)
            {
                //BEMultiBlockTexture bembt = capi.World.BlockAccessor.GetBlockEntity(Pos.Copy().Offset(bf)) as BEMultiBlockTexture;
                
                BEConnectedTextures otherblock= capi.World.BlockAccessor.GetBlockEntity(Pos.Copy().Offset(bf)) as BEConnectedTextures;
                if (otherblock==null) { neighbors[bf] = false; }
                else {
                    neighbors[bf] = true;
                    
                }
            }
            //Do a check to see if neighbor set has changed if it hasn't changed we don't have to make a new mesh
            if (oldneighbors == null)
            {
                oldneighbors = new Dictionary<BlockFacing, bool>(neighbors);
            }
            else
            {
                bool mismatch = false;
                foreach (BlockFacing bf in BlockFacing.ALLFACES)
                {
                    if (oldneighbors[bf] != neighbors[bf]) { mismatch = true;break; }
                }
                if (!mismatch && meshref!=null) { return; }
            }
            if (meshref != null) { capi.Render.DeleteMesh(meshref); }
            meshref?.Dispose();
            meshref = null;
            meshdata = new MeshData();
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
                

            }
            capi.Tesselator.TesselateShape("tileconcrete" + Pos.ToString(), shape, out meshdata, this);

            //MarkDirty(true);
        }



        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {

            if (meshdata == null) { return base.OnTesselation(mesher, tessThreadTesselator); }
            mesher.AddMeshData(meshdata);
            try
            {
                meshref = capi.Render.UploadMesh(meshdata);
            }
            catch
            {

            }
            return true;
        }


        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);

        }

        public override void OnBlockRemoved()
        {
            if (capi == null) { base.OnBlockRemoved();return; }
            if (capi != null && meshref != null) { capi.Render.DeleteMesh(meshref); }

            meshref?.Dispose();
            meshref = null;
            
            base.OnBlockRemoved();
        }
    }

    
}