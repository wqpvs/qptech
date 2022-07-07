using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.API.Config;

namespace qptech.src.procanim
{
    class BEBProceduralAnimator:BlockEntityBehavior
    {
        ICoreClientAPI capi;
        BlockEntity blockEntity;
        public List<ProceduralAnimation> procanimlist;
        string currentanimationname = "idle";
        int currentanimationframe = 0;
        int currentanimationkey = 0;
        bool animationon = true;
        ProceduralAnimation pani;
        public BEBProceduralAnimator(BlockEntity be) : base(be)
        {
            blockEntity = be;
        }
        Block partblock;
        
        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            if (api is ICoreClientAPI)
            {
                
                capi = api as ICoreClientAPI;
                blockEntity.RegisterGameTickListener(OnClientTick, 25);
                procanimlist = new List<ProceduralAnimation>();
                ProceduralAnimation idle = new ProceduralAnimation();
                idle.name = "idle";
                idle.shapefrom = "machines:cuttingmachineblade";
                ProceduralAnimationKey idlekey = new ProceduralAnimationKey();
                idlekey.framelength = 1;
                idle.frames.Add(idlekey);
                procanimlist.Add(idle);
                ProceduralAnimation test = new ProceduralAnimation();
                test.name = "run";
                test.shapefrom = "machines:cuttingmachineblade";
                ProceduralAnimationKey testkey = new ProceduralAnimationKey();
                testkey.framelength = 5;
                testkey.transformstart = new ModelTransform();
                testkey.transformend = new ModelTransform();
                testkey.transformstart.Origin = testkey.transformend.Origin= new Vec3f(0.5f, 0.45f, 0.14f);
                testkey.transformstart.Rotation = new Vec3f(0, 0, 0);
                testkey.transformend.Rotation = new Vec3f(90, 0,0);
                test.frames.Add(testkey);
                ProceduralAnimationKey testkey2 = new ProceduralAnimationKey();
                testkey2.framelength = 15;
                testkey2.transformstart = new ModelTransform();
                testkey2.transformend = new ModelTransform();
                testkey2.transformstart.Origin = testkey.transformend.Origin = new Vec3f(0.5f, 0.45f, 0.14f);
                testkey2.transformstart.Rotation = new Vec3f(90, 0,0);
                testkey2.transformend.Rotation = new Vec3f(0, 0, 0);
                test.frames.Add(testkey2);
                procanimlist.Add(test);
                
            }
        }
        public void OnClientTick(float df)
        {
           
            GenMesh();
            blockEntity.MarkDirty(true);
        }
        MeshData meshdata;
        
        void GenMesh()
        {

            if (pani == null)
            {
                if (procanimlist == null || procanimlist.Count == 0) { return; }
                pani = procanimlist.Find(p => p.name == currentanimationname);
                if (pani == null) { return; }
            }
            AssetLocation al = new AssetLocation(pani.shapefrom);
            Block partblock = capi.World.GetBlock(al);
            if (partblock == null) {
                return;
            }
            capi.Tesselator.TesselateBlock(partblock, out meshdata);
            ModelTransform nowtransform = new ModelTransform();
            ModelTransform s = pani.frames[currentanimationkey].transformstart;
            ModelTransform e = pani.frames[currentanimationkey].transformend;
            if (s == null || e == null) { return; }
            float anipct = (float)currentanimationframe / (float)pani.frames[currentanimationkey].framelength;
            if (s.Origin != null)
            {
                nowtransform.Origin = s.Origin;
            }
            if (s.Rotation != null && e.Rotation != null)
            {
                nowtransform.Rotation = LerpVec3f(s.Rotation, e.Rotation, anipct);
                meshdata.Rotate(s.Origin, nowtransform.Rotation.X * GameMath.DEG2RAD, nowtransform.Rotation.Y * GameMath.DEG2RAD, nowtransform.Rotation.Z * GameMath.DEG2RAD);
            }
            //update animation frame
            currentanimationframe++;
            if (currentanimationframe > pani.frames[currentanimationkey].framelength)
            {
                currentanimationframe = 0;
                currentanimationkey++;
                if (currentanimationkey >= pani.frames.Count) { currentanimationkey = 0; }
            }
        }
        public static Vec3f LerpVec3f(Vec3f startv, Vec3f endv, float pct)
        {
            
            if (pct <=0) { return startv; }
            else if (pct >= 1) { return endv; }
            Vec3f output = new Vec3f(LerpFloat(startv.X,endv.X,pct), LerpFloat(startv.Y, endv.Y, pct), LerpFloat(startv.Z, endv.Z, pct));
            return output;
        }
        public static float LerpFloat(float startf, float endf, float pct)
        {
            if (startf == endf) { return startf; }
            if (pct <= 0) { return startf; }
            else if (pct >= 1) { return endf; }
            float d = endf - startf;
            d = startf + d * pct;
            return d;
        }
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            try { mesher.AddMeshData(meshdata); }
            catch { return base.OnTesselation(mesher, tessThreadTesselator); }

            return base.OnTesselation(mesher, tessThreadTesselator);
            
        }
        public void SetProcAnimation(string newanimation)
        {
            if (procanimlist == null | procanimlist.Count == 0) { return; }
            if (currentanimationname == newanimation) { return; } //already running animation
            ProceduralAnimation newpa = procanimlist.Find(p => p.name == newanimation);
            if (newpa == null) { return; }
            pani = newpa;
            currentanimationname = newanimation;
            currentanimationframe = 0;
            currentanimationkey = 0;
        }
    }

    public class ProceduralAnimation
    {
        public string name;
        public string shapefrom;
        public List<ProceduralAnimationKey> frames;
        public ProceduralAnimation() { frames = new List<ProceduralAnimationKey>(); }
    }
    //actually keys should only need transform end and could reference the last key
    public class ProceduralAnimationKey
    {
        public int framelength;
        public ModelTransform transformstart;
        public ModelTransform transformend;
        public ProceduralAnimationKey() { }
    }
    
}
