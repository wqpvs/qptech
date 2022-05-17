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

namespace modernblocks.src
{
    class TestRenderer:IRenderer
    {
        
        BlockPos pos;
        ICoreClientAPI api;
        MeshRef quadModelRef;
        
        Matrixf ModelMat = new Matrixf();
        
        public double RenderOrder
        {
            get { return 0.5; }
        }

        public int RenderRange
        {
            get { return 24; }
        }

        string[] textures;

        public AssetLocation TextureName = null;

        public TestRenderer(BlockPos pos, ICoreClientAPI api)
        {
            this.pos = pos;
            this.api = api;

            GenModel();
        }
        #region meshbuildingdata
        // N: Z=0, S: Z=1
        // W: X=0, E: X=1
        // D: Y=0, U: Y=1
        static float ts = 0.25f;// 1f / 16f/4f;
        
        static readonly float[] cubeVertices =
        {
            0,0,0, //WDN 0
            0,0,1, //WDS 1
            0,1,0, //WUN 2
            0,1,1, //WUS 3
            
 
        };

        
        #endregion
        public void GenModel()
        {
            quadModelRef?.Dispose();

            //
            //
            //
            //

            //these are the UV coordinates
            //normal textures are mapped on 16 = full texture (from the voxel measurements)
            //these textures are 128 or 4x4 texture units



            float[] quadTextureCoords = {
                ts,ts, //0,0,0
                ts,0, //0,0,1
                0,ts, //0,1,0
                0,0  //0,1,1
            };
            
            //this is the pattern to build the mesh, two triangles
            int[] quadVertexIndices = {
                //0, 4, 6,    0,6,2, 
                0,1,3,0,3,2
            };

            

            MeshData m = new MeshData();

            //XYZ these are the vertices on our cube
            float[] xyz = new float[cubeVertices.Length];
            for (int i = 0; i < cubeVertices.Length; i++)
            {
                xyz[i] = cubeVertices[i];
            }
            m.SetXyz(xyz);

            //Set the UV coordinates for the triangles
            float[] uv = new float[quadTextureCoords.Length];
            for (int i = 0; i < uv.Length; i++)
            {
                uv[i] = quadTextureCoords[i];
            }
            m.SetUv(uv);
            m.SetVerticesCount(cubeVertices.Length);
            m.SetIndices(quadVertexIndices);
            m.SetIndicesCount(quadVertexIndices.Length);
            m.Rgba = new byte[quadTextureCoords.Length];
            m.Rgba.Fill((byte)255);
            m.Flags = new int[quadTextureCoords.Length];




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
