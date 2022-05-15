using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace modernblocks
{
    class ConnectedTextureRenderer : IRenderer, ITexPositionSource
    {
        ICoreClientAPI capi;
        private BlockPos pos;
        MeshRef mainmesh;
        public double RenderOrder
        {
            get { return 0.5; }
        }

        public int RenderRange
        {
            get { return 24; }
        }

        public Size2i AtlasSize
        {
            get { return capi.BlockTextureAtlas.Size; }
        }
        int textureId;
        ITexPositionSource tmpTextureSource;
        string tmpMat;
        public TextureAtlasPosition this[string textureCode]
        {
            get { return tmpTextureSource[tmpMat]; }
        }
        Matrixf ModelMat = new Matrixf();
        public ConnectedTextureRenderer(BlockPos pos, ICoreClientAPI capi, string elementName)
        {
            this.pos = pos;
            this.capi = capi;
            Block elementblock = capi.World.GetBlock(new AssetLocation(elementName));
            //elementtexpos = capi.BlockTextureAtlas.GetPosition(elementblock, "element");

            mainmesh = capi.Render.UploadMesh(capi.TesselatorManager.GetDefaultBlockMesh(elementblock));
        }
        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {


            IRenderAPI rpi = capi.Render;
            IClientWorldAccessor worldAccess = capi.World;
            Vec3d camPos = worldAccess.Player.Entity.CameraPos;

            rpi.GlDisableCullFace();
            IStandardShaderProgram prog = rpi.StandardShader;
            prog.Use();
            prog.RgbaAmbientIn = rpi.AmbientColor;
            prog.RgbaFogIn = rpi.FogColor;
            prog.FogMinIn = rpi.FogMin;
            prog.FogDensityIn = rpi.FogDensity;
            prog.RgbaTint = ColorUtil.WhiteArgbVec;
            prog.DontWarpVertices = 0;
            prog.AddRenderFlags = 0;
            prog.ExtraGodray = 0;
            prog.OverlayOpacity = 0;


            if (mainmesh != null)
            {


                Vec4f lightrgbs = capi.World.BlockAccessor.GetLightRGBs(pos.X, pos.Y, pos.Z);

                prog.NormalShaded = 1;
                prog.RgbaLightIn = lightrgbs;

                prog.Tex2D = textureId;
                prog.ModelMatrix = ModelMat.Identity().Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z).Values;
                prog.ViewMatrix = rpi.CameraMatrixOriginf;
                prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;

                rpi.RenderMesh(mainmesh);

            }




            prog.Stop();
        }

        public void Dispose()
        {
            capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
            //elementMeshRef?.Dispose();
            //coalQuadRef?.Dispose();
            mainmesh?.Dispose();
        }
    }
}