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
using Vintagestory.API.Util;

namespace qptech.src
{
    class WireRenderer : IRenderer
    {
        static int[] quadTextureCoords = {
            0, 0,
            1, 0,
            1, 1,
            0, 1
        };

        static int[] quadVertexIndices = {
            0, 1, 2,    0, 2, 3
        };
        BlockPos pos;
        ICoreClientAPI api;
        MeshRef quadModelRef;
        public float wirethickness = 0.05f;
        public Vec3f wireoffset;
        Matrixf ModelMat = new Matrixf();
        public BEElectric bee;
        public double RenderOrder
        {
            get { return 0.5; }
        }

        public int RenderRange
        {
            get { return 24; }
        }

        

        public AssetLocation TextureName = null;
        
        public WireRenderer(BlockPos pos, ICoreClientAPI api)
        {
            this.pos = pos;
            this.api = api;
            
            GenModel();
        }

        public void GenModel()
        {
            quadModelRef?.Dispose();

            if (bee == null || bee.DirectLinks == null || bee.DirectLinks.Count == 0) { return; }

            /*MeshData modeldata = QuadMeshUtil.GetQuad();
            modeldata.Uv = new float[]
            {
            3/16f, 7/16f,
            0, 7/16f,
            0, 0,
            3/16f, 0
            };

            modeldata.Rgba = new byte[4 * 4];
            modeldata.Rgba.Fill((byte)255);
            modeldata.Flags = new int[4 * 4];*/

            MeshData m = new MeshData();
            
            BlockPos d = bee.DirectLinks[0]-bee.Pos;
            if (wireoffset == null) { wireoffset = new Vec3f(0.5f, 0.5f, 0.5f); }
            Vec3f doffset = wireoffset;
            BEElectric dest = api.World.BlockAccessor.GetBlockEntity(bee.DirectLinks[0]) as BEElectric;
            
            if (dest != null&&dest.wireoffset!=null)
            {
                doffset = dest.wireoffset;
            }
            
            float[] quadVertices = {
            
            wireoffset.X,wireoffset.Y,wireoffset.Z,
            wireoffset.X,wireoffset.Y+wirethickness,wireoffset.Z,
            doffset.X+d.X,doffset.Y+d.Y,doffset.Z + d.Z,
            doffset.X + d.X,doffset.Y+d.Y+wirethickness,doffset.Z + d.Z
            };
            float[] xyz = new float[3 * 4];
            for (int i = 0; i < 3 * 4; i++)
            {
                xyz[i] = quadVertices[i];
            }
            m.SetXyz(xyz);
            float[] uv = new float[2 * 4];
            for (int i = 0; i < uv.Length; i++)
            {
                uv[i] = quadTextureCoords[i];
            }
            m.SetUv(uv);
            m.SetVerticesCount(4);
            m.SetIndices(quadVertexIndices);
            m.SetIndicesCount(3 * 2);



            m.Uv = new float[]
            {
            3/16f, 7/16f,
            0, 7/16f,
            0, 0,
            3/16f, 0
            };

            m.Rgba = new byte[4 * 4];
            m.Rgba.Fill((byte)255);
            m.Flags = new int[4 * 4];

            
            

            quadModelRef = api.Render.UploadMesh(m);
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (quadModelRef == null) { return; }

            IRenderAPI rpi = api.Render;
            IClientWorldAccessor worldAccess = api.World;
            EntityPos plrPos = worldAccess.Player.Entity.Pos;
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
            prog.ExtraGodray = 0;
            prog.AddRenderFlags = 0;

            
                Vec4f lightrgbs = api.World.BlockAccessor.GetLightRGBs(pos.X, pos.Y, pos.Z);
                
                int extraGlow = 0;

                prog.RgbaLightIn = lightrgbs;
                

                prog.ExtraGlow = extraGlow;
                prog.NormalShaded = 0;

                int texid = api.Render.GetOrLoadTexture(TextureName);
                rpi.BindTexture2d(texid);

               

            prog.ModelMatrix = ModelMat
                .Identity()
                .Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z)
                //.Translate(wireoffset.X, wireoffset.Y, wireoffset.Z)
                
                //.Scale(1, 0.025f, 1)
                    //.Translate(xzOffset / 16f, 1 / 16f , 8.5f / 16)
                    //.RotateX(90 * GameMath.DEG2RAD)
                    //.Scale(0.5f * 3 / 16f, 0.5f * 7 / 16f, 0.5f)
                    .Values
                ;
                prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
                prog.ViewMatrix = rpi.CameraMatrixOriginf;
                if (quadModelRef.Disposed) { prog.Stop(); return; }
                rpi.RenderMesh(quadModelRef);
            


            prog.Stop();
            rpi.GlEnableCullFace();
        }

        public void Dispose()
        {
            api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);

            quadModelRef?.Dispose();
        }
    }    
}
