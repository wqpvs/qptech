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
    /// <summary>
    /// TODO:
    /// - break apart the cube and UV array code and assemble on the fly
    ///    - (needs to accept code about which faces to show/hide - probably let the master block decide that)
    /// - when sending which faces to show, it should maybe send the hash of directions, which will be converted into appropriate UV offsets
    /// - will also accept fill colors
    /// - random frame animation mode
    /// - frame range animation mode?
    /// </summary>
    class ConnectedTextureRenderer : IRenderer
    {

        BlockPos pos;
        ICoreClientAPI capi;
        MeshRef cubeModelRef;
        static MeshRef baseCubeModelRef;
        Matrixf ModelMat = new Matrixf();


        public double RenderOrder
        {
            get { return 0.5; }
        }

        public int RenderRange
        {
            get { return 24; }
        }

        public List<FaceData> facedata;

        public AssetLocation TextureName = null;

        public ConnectedTextureRenderer(BlockPos pos, ICoreClientAPI api)
        {
            this.pos = pos;
            this.capi = api;
            if (baseCubeModelRef == null || baseCubeModelRef.Disposed || !baseCubeModelRef.Initialized)
            {
                MeshData mesh = api.TesselatorManager.GetDefaultBlockMesh(api.World.BlockAccessor.GetBlock(pos));
                baseCubeModelRef = api.Render.UploadMesh(mesh);
            }
            capi.Event.EnqueueMainThreadTask(() =>
            {
                capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "bemodern");
                capi.Event.RegisterRenderer(this, EnumRenderStage.ShadowFar, "bemodern");
                capi.Event.RegisterRenderer(this, EnumRenderStage.ShadowNear, "bemodern");
            }, "registerrenderers");

        }
        #region meshbuildingdata

        //these define the points on the various cube faces
        static readonly Dictionary<BlockFacing, List<float>> cubeVertexLookup = new Dictionary<BlockFacing, List<float>>() {
            { BlockFacing.WEST,new List<float>() { 0, 0, 0, 0, 0, 1, 0, 1, 0, 0, 1, 1 } },
            { BlockFacing.NORTH,new List<float>() { 0, 0, 0, 0, 1, 0, 1, 0, 0, 1, 1, 0, } },
            { BlockFacing.EAST,new List<float>() { 1, 0, 0, 1, 0, 1, 1, 1, 0, 1, 1, 1 } },
            { BlockFacing.SOUTH,new List<float>() { 0, 0, 1, 0, 1, 1, 1, 0, 1, 1, 1, 1, } },
            { BlockFacing.UP,new List<float>() { 0, 1, 0, 0, 1, 1, 1, 1, 0, 1, 1, 1, } },
            { BlockFacing.DOWN,new List<float>() { 0, 0, 0, 0, 0, 1, 1, 0, 0, 1, 0, 1 } }
        };


        #endregion

        int texid = 0;
        public void GenModel()
        {
            cubeModelRef?.Dispose();
            if (facedata == null||facedata.Count==0) { return; }
            texid = capi.Render.GetOrLoadTexture(TextureName);

            Random r = new Random();
            
            List<float> cubeVertices = new List<float>();
            List<int> cubeindices = new List<int>();
            List<float> cubeUVs = new List<float>();
            int vc = 0;
            
            foreach (FaceData fd in facedata)
            {
                BlockFacing tbf = fd.facing;
                
                float u1 = fd.ucell * fd.cellsize;
                float u2 = u1 + fd.cellsize;
                float v1 = fd.vcell * fd.cellsize;
                float v2 = v1 + fd.cellsize;
                cubeVertices.AddRange(cubeVertexLookup[tbf]);
                cubeindices.AddRange(new List<int>() { 3 + vc * 4, 1 + vc * 4, 0 + vc * 4, 2 + vc * 4, 0 + vc * 4, 3 + vc * 4 });
                vc++;
                if (tbf == BlockFacing.WEST)
                {
                    cubeUVs.AddRange(new List<float>() {u1 ,v2 ,
                u2 ,v2 ,
                u1 ,v1 ,
                u2 ,v1 });
                }
                else if (tbf == BlockFacing.NORTH)
                {
                    cubeUVs.AddRange(new List<float>() { u2 ,v2 ,
                u2 ,v1 ,
                u1 ,v2 ,
                u1 ,v1});
                }
                else if (tbf == BlockFacing.EAST)
                {
                    cubeUVs.AddRange(new List<float>() { u2 ,v2 ,
                u1 ,v2 ,
                u2 ,v1 ,
                u1 ,v1});
                }
                else if (tbf == BlockFacing.SOUTH)
                {
                    cubeUVs.AddRange(new List<float>() {u1 ,v2 ,
                u1, v1 ,
                u2 ,v2,
                u2 ,v1 });
                }
                else if (tbf == BlockFacing.UP)
                {
                    cubeUVs.AddRange(new List<float>() { u1, v1 ,
                u1 ,v2 ,
                u2 ,v1 ,
                u2 ,v2 });
                }
                else if (tbf == BlockFacing.DOWN)
                {
                    cubeUVs.AddRange(new List<float>() {u1, v2 ,
                u1 ,v1 ,
                u2 ,v2 ,
                u2 ,v1 });
                }
            }
            int numVerts = cubeVertices.Count / 3;
            MeshData m = new MeshData();

            //XYZ these are the vertices on our cube
            float[] xyz = new float[cubeVertices.Count];
            for (int i = 0; i < cubeVertices.Count; i++)
            {
                xyz[i] = cubeVertices[i];
            }
            m.SetXyz(xyz);

            //Set the UV coordinates for the triangles
            float[] uv = new float[cubeUVs.Count];
            for (int i = 0; i < uv.Length; i++)
            {
                uv[i] = cubeUVs[i];
            }
            m.SetUv(uv);
            m.SetVerticesCount(cubeVertices.Count);
            m.SetIndices(cubeindices.ToArray());
            m.SetIndicesCount(cubeindices.Count);
            
            
            m.Rgba = new byte[numVerts*4]; //colored vertex shading

            FaceData usedata = facedata[0];
             for (int cc=0; cc < numVerts*4; cc += 4)
            {
                
                m.Rgba[cc] = usedata.rgba[0];
                m.Rgba[cc + 1] = usedata.rgba[1];
                m.Rgba[cc + 2] = usedata.rgba[2];
                m.Rgba[cc + 3] = usedata.rgba[3];
            }
            

            m.Flags = new int[numVerts*4]; //not clear on what flags do
            cubeModelRef = capi.Render.UploadMesh(m);
        }
        public float[] ModelMatf = Mat4f.Create();
        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (cubeModelRef == null|| !cubeModelRef.Initialized||cubeModelRef.Disposed) { return; }
            bool shadowPass = stage != EnumRenderStage.Opaque;
            
            IRenderAPI rpi = capi.Render;
            IShaderProgram prevProg = rpi.CurrentActiveShader;
            prevProg?.Stop();
            IClientWorldAccessor worldAccess = capi.World;
            EntityPos plrPos = worldAccess.Player.Entity.Pos;
            Vec3d camPos = worldAccess.Player.Entity.CameraPos;

            rpi.GlDisableCullFace();
            
            IStandardShaderProgram prog = rpi.StandardShader;
            prog.Use();

            if (!shadowPass)
            {
                prog.RgbaAmbientIn = rpi.AmbientColor;
                prog.RgbaFogIn = rpi.FogColor;
                prog.FogMinIn = rpi.FogMin;
                prog.FogDensityIn = rpi.FogDensity;
                prog.RgbaTint = ColorUtil.WhiteArgbVec;
                prog.DontWarpVertices = 0;
                prog.ExtraGodray = 0;
                prog.AddRenderFlags = 0;
                Vec4f lightrgbs = capi.World.BlockAccessor.GetLightRGBs(pos.X, pos.Y, pos.Z);

                int extraGlow = 0;

                prog.RgbaLightIn = lightrgbs;


                prog.ExtraGlow = extraGlow;
                prog.NormalShaded = 0;
            }

            
            rpi.BindTexture2d(texid);

            prog.ModelMatrix = ModelMat
                .Identity()
                .Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z)

                    .Values
                ;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
            if (shadowPass)
            {
                prog.ViewMatrix = Mat4f.Mul(new float[16], capi.Render.CurrentModelviewMatrix, ModelMatf);
                rpi.RenderMesh(baseCubeModelRef);
            }
            else
            {
                
                prog.ViewMatrix = rpi.CameraMatrixOriginf;
                rpi.RenderMesh(cubeModelRef);
            }
            
            prog.Stop();
            prevProg?.Use();
            rpi.GlEnableCullFace();
        }

        public void Dispose()
        {
            capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
            capi.Event.UnregisterRenderer(this, EnumRenderStage.ShadowFar);
            capi.Event.UnregisterRenderer(this, EnumRenderStage.ShadowNear);
            

            cubeModelRef?.Dispose();
        }
    }

    
}
