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
    class TestRenderer : IRenderer
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

        public List<FaceData> facedata;

        public AssetLocation TextureName = null;

        public TestRenderer(BlockPos pos, ICoreClientAPI api)
        {
            this.pos = pos;
            this.api = api;

            GenModel();
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
        public void GenModel()
        {
            quadModelRef?.Dispose();
            if (facedata == null||facedata.Count==0) { return; }
           

            Random r = new Random();
            FaceData usedata = facedata[0];
            float u1 = usedata.ucell*usedata.cellsize;
            float u2 = u1+usedata.cellsize;
            float v1 = usedata.vcell*usedata.cellsize;
            float v2 = v1+usedata.cellsize;
            List<float> cubeVertices = new List<float>();
            List<int> cubeindices = new List<int>();
            List<float> cubeUVs = new List<float>();
            int vc = 0;
            
            cubeVertices.AddRange(cubeVertexLookup[BlockFacing.WEST]);
            cubeindices.AddRange(new List<int>() { 3+vc*4, 1 + vc * 4, 0 + vc * 4, 2 + vc * 4, 0 + vc * 4, 3 + vc * 4 });
            vc++;

            cubeVertices.AddRange(cubeVertexLookup[BlockFacing.NORTH]);
            cubeindices.AddRange(new List<int>() { 3 + vc * 4, 1 + vc * 4, 0 + vc * 4, 2 + vc * 4, 0 + vc * 4, 3 + vc * 4 });
            vc++;
            cubeVertices.AddRange(cubeVertexLookup[BlockFacing.EAST]);
            cubeindices.AddRange(new List<int>() { 3 + vc * 4, 1 + vc * 4, 0 + vc * 4, 2 + vc * 4, 0 + vc * 4, 3 + vc * 4 });
            vc++;
            cubeVertices.AddRange(cubeVertexLookup[BlockFacing.SOUTH]);
            cubeindices.AddRange(new List<int>() { 3 + vc * 4, 1 + vc * 4, 0 + vc * 4, 2 + vc * 4, 0 + vc * 4, 3 + vc * 4 });
            vc++;
            cubeVertices.AddRange(cubeVertexLookup[BlockFacing.UP]);
            cubeindices.AddRange(new List<int>() { 3 + vc * 4, 1 + vc * 4, 0 + vc * 4, 2 + vc * 4, 0 + vc * 4, 3 + vc * 4 });
            vc++;
            cubeVertices.AddRange(cubeVertexLookup[BlockFacing.DOWN]);
            cubeindices.AddRange(new List<int>() { 3 + vc * 4, 1 + vc * 4, 0 + vc * 4, 2 + vc * 4, 0 + vc * 4, 3 + vc * 4 });
            vc++;
            //I somehow figured this out by repeatedly facerolling on the keyboard face by face until it worked
            float[] quadTextureCoords = {
                //WEST
                u1 ,v2 ,
                u2 ,v2 ,
                u1 ,v1 ,
                u2 ,v1,
                //NORTH
                u2 ,v2 , 
                u2 ,v1 , 
                u1 ,v2 , 
                u1 ,v1,
                //EAST
                u2 ,v2 , 
                u1 ,v2 , 
                u2 ,v1 , 
                u1 ,v1,
                //SOUTH
                u1 ,v2 ,
                u1, v1 ,
                u2 ,v2,
                u2 ,v1 ,
                //UP
                u1, v1 ,
                u1 ,v2 ,
                u2 ,v1 ,
                u2 ,v2,
                //DOWN
                u1, v2 ,
                u1 ,v1 ,
                u2 ,v2 ,
                u2 ,v1,
            };
            
            //this is the pattern to build the mesh, two triangles
            //the vertex order for each triangle doesn't matter
            /*int[] quadVertexIndices = {
                //0, 4, 6,    0,6,2, 
                3,1,0,2,0,3, //West
                3+4,1+4,4,2+4,4,3+4, //N
                3+8,1+8,8,2+8,8,3+8, //E
                3+12,1+12,12,2+12,12,3+12, //S
                3+16,1+16,16,2+16,16,3+16, //U
                3+20,1+20,20,2+20,20,3+20 //U
            };*/
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
            float[] uv = new float[quadTextureCoords.Length];
            for (int i = 0; i < uv.Length; i++)
            {
                uv[i] = quadTextureCoords[i];
            }
            m.SetUv(uv);
            m.SetVerticesCount(cubeVertices.Count);
            m.SetIndices(cubeindices.ToArray());
            m.SetIndicesCount(cubeindices.Count);
            
            
            m.Rgba = new byte[numVerts*4]; //colored vertex shading

            
             for (int cc=0; cc < numVerts*4; cc += 4)
            {
                
                m.Rgba[cc] = usedata.rgba[0];
                m.Rgba[cc + 1] = usedata.rgba[1];
                m.Rgba[cc + 2] = usedata.rgba[2];
                m.Rgba[cc + 3] = usedata.rgba[3];
            }
            

            m.Flags = new int[numVerts*4]; //not clear on what flags do
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
