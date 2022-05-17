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
            //WEST QUAD
            0,0,0, //WDN 0
            0,0,1, //WDS 1
            0,1,0, //WUN 2
            0,1,1, //WUS 3
            //NORTH QUAD
            0,0,0, //WDN 4
            0,1,0, //EDN 5
            1,0,0, //EUN 6
            1,1,0, //WUN 7
            //EAST QUAD
            1,0,0, //EDN 8
            1,0,1, //EDS 9
            1,1,0, //EUN 10
            1,1,1, //EUS 11
            //SOUTH QUAD
            0,0,1, //WDN 12
            0,1,1, //EDN 13
            1,0,1, //EUN 14
            1,1,1, //WUN 15
 
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

            float oo = 0;

            float[] quadTextureCoords = {
                
                //ts ,ts , // 0,0,0  This pattern has NW rotated wrong (90 CW from where it should be)
                //ts ,oo , // 0,0,1
                //oo ,ts , // 0,1,0
                //oo ,oo  //  0,1,1

                //ts, oo , // 0,0,0  This pattern has NW rotate 90 CCW from where it should be  and mirrored
                //ts ,ts , // 0,0,1
                //oo ,oo , // 0,1,0
                //oo ,ts  //  0,1,1

                //ts ,ts , // 0,0,0  This pattern is mirrored
                //oo ,ts , // 0,0,1
                //ts ,oo , // 0,1,0
                //oo ,oo  //  0,1,1
                //WEST
                oo ,ts , // 0,0,0  
                ts ,ts , // 0,0,1
                oo ,oo , // 0,1,0
                ts ,oo,
                //NORTH
                ts ,ts , 
                ts ,oo , 
                oo ,ts , 
                oo ,oo,
                //EAST
                ts ,ts , 
                oo ,ts , 
                ts ,oo , 
                oo ,oo,
                //SOUTH
                
                oo ,ts ,
                oo, oo ,
                
                ts ,ts,
                ts ,oo ,

            };
            
            //this is the pattern to build the mesh, two triangles
            //the vertex order for each triangle doesn't matter
            int[] quadVertexIndices = {
                //0, 4, 6,    0,6,2, 
                3,1,0,2,0,3, //West
                3+4,1+4,4,2+4,4,3+4, //N
                3+8,1+8,8,2+8,8,3+8, //E
                3+12,1+12,12,2+12,12,3+12 //S
            };
            int numVerts = cubeVertices.Length / 3;
            

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
            m.Rgba = new byte[numVerts*4];
            m.Rgba.Fill((byte)255);
            m.Flags = new int[numVerts*4];




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
