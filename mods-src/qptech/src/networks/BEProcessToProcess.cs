using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.API.Client;
using qptech.src;


namespace qptech.src.networks
{
    class BEProcessToProcess : BlockEntity, IProcessingSupplier
    {
        public Dictionary<string, double> suppliedProcesses;
        public Dictionary<string, double> requiredProcesses;
        Dictionary<string, double> missing;
        bool missingprocesses = false;
        string missingprocesstext = "";
        List<BlockFacing> processInputFaces;
        ILoadedSound ambientSound;
        string runsound = "";
        float soundlevel = 0.5f;
        bool loopsound;
        bool alreadyPlayedSound;
        bool inUse = false;
        bool onlyAnimateIfInUse = true;
        bool ShouldAnimate => Running && (inUse||!onlyAnimateIfInUse);
        bool CanAnimate => animationCode != "";
        double suspendCoolDown = 1000; //how many ms after last use until stopping animations
        double lastUseAt = 0; //last time it was used
        double stopAnimatingAt => lastUseAt + suspendCoolDown;
        protected string animationCode = "";
        protected string animation = "";
        protected float runAnimationSpeed = 1;
        public virtual bool Running => !missingprocesses;
        
        public virtual float SoundLevel
        {
            get { return soundlevel; }
        }
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (Block.Attributes != null)
            {
                suppliedProcesses = new Dictionary<string, double>();
                suppliedProcesses = Block.Attributes["processes"].AsObject<Dictionary<string, double>>();
                requiredProcesses = new Dictionary<string, double>();
                requiredProcesses = Block.Attributes["requiredProcesses"].AsObject<Dictionary<string, double>>();
                runsound = Block.Attributes["runsound"].AsString(runsound);
                soundlevel = Block.Attributes["soundlevel"].AsFloat(soundlevel);
                loopsound = Block.Attributes["loopsound"].AsBool(loopsound);
                animationCode = Block.Attributes["animationCode"].AsString(animationCode);
                animation = Block.Attributes["animation"].AsString(animation);
                runAnimationSpeed = Block.Attributes["runAnimationSpeed"].AsFloat(runAnimationSpeed);
                onlyAnimateIfInUse = Block.Attributes["onlyAnimateIfInUse"].AsBool(onlyAnimateIfInUse);
                suspendCoolDown = Block.Attributes["suspendCoolDown"].AsDouble(suspendCoolDown);
                if (!Block.Attributes.KeyExists("processInputFaces")) { processInputFaces = BlockFacing.ALLFACES.ToList<BlockFacing>(); }
                else
                {
                    string[] cfaces = Block.Attributes["processInputFaces"].AsArray<string>();
                    processInputFaces = new List<BlockFacing>();
                    foreach (string f in cfaces)
                    {
                        processInputFaces.Add(BEElectric.OrientFace(Block.Code.ToString(), BlockFacing.FromCode(f)));
                    }
                }
                RegisterGameTickListener(OnTick, 100);
                if (api.World.Side == EnumAppSide.Client && animationCode!="")
                {
                    float rotY = Block.Shape.rotateY;
                    animUtil.InitializeAnimator("BEProcessToProcess", new Vec3f(0, rotY, 0));


                }
            }
        }
        private SimpleParticleProperties smokeParticles;
        public virtual void OnTick(float df)
        {
            if (Api is ICoreServerAPI) {
                CheckRequiredProcesses(); 
                if (Running && Api.World.ElapsedMilliseconds > stopAnimatingAt) { inUse = false; MarkDirty(); }
            }
            if (CanAnimate)
            {
                DoRunningParticles();
            }
            ToggleAmbientSounds(Running);
            
            if (Api is ICoreClientAPI && CanAnimate) { DoAnimations(); }
        }
        public virtual void DoRunningParticles()
        {
            //Temp code for steam particles, def needs to be moved to json
            if (suppliedProcesses.ContainsKey("steam")){

                smokeParticles = new SimpleParticleProperties(
                      0, 2,
                      ColorUtil.ToRgba(64, 255, 255, 255),
                      new Vec3d(0, 28, 0),
                      new Vec3d(0.75, 32, 0.75),
                      new Vec3f(-1 / 32f, 0.2f, -1 / 32f),
                      new Vec3f(1 / 32f, 1f, 1 / 32f),
                      1.5f,
                      -0.025f / 4,
                      0.2f,
                      0.6f,
                      EnumParticleModel.Quad
                  );

                smokeParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.25f);
                smokeParticles.SelfPropelled = true;
                smokeParticles.AddPos.Set(8 / 16.0, 0, 8 / 16.0);
                smokeParticles.MinPos.Set(Pos.X + 4 / 16f, Pos.Y + 3 / 16f, Pos.Z + 4 / 16f);
                Api.World.SpawnParticles(smokeParticles);
            }
        }

        public virtual bool RequestProcessing(string process, double strength)
        {
            if (suppliedProcesses == null||requiredProcesses==null) { return false; }
            
            if (!suppliedProcesses.ContainsKey(process)) { return false; }
            if (!CheckRequiredProcesses()) { return false; }
            if (suppliedProcesses[process] < strength) { return false; }
            lastUseAt = Api.World.ElapsedMilliseconds;
            inUse = true;
            MarkDirty();
            return true;
        }

        public virtual double RequestProcessing(string process)
        {
            if (suppliedProcesses == null || requiredProcesses == null) { return 0; }
            if (!suppliedProcesses.ContainsKey(process)) { return 0; }
            if (!CheckRequiredProcesses()) { return 0; }
            lastUseAt = Api.World.ElapsedMilliseconds;
            inUse = true;
            MarkDirty();
            return suppliedProcesses[process];
        }
        public bool CheckProcessing(string process)
        {
            if (suppliedProcesses == null) { return false; }
            if (suppliedProcesses.ContainsKey(process)) { return true; }
            return false;
        }
        protected virtual bool CheckRequiredProcesses()
        {
            missing = new Dictionary<string, double>(requiredProcesses);
            missingprocesses = false;
            if (missing.Count == 0) { MarkDirty(); return true; }
            BlockFacing[] checkfaces = processInputFaces.ToArray();
            foreach (BlockFacing bf in checkfaces)
            {
                
                IProcessingSupplier ips = Api.World.BlockAccessor.GetBlockEntity(Pos.Copy().Offset(bf)) as IProcessingSupplier;
                if (ips == null) { continue; }
                foreach (string checkprocess in missing.Keys.ToArray<string>())
                {
                    if (missing[checkprocess] <= 0) { continue; }
                    missing[checkprocess] -= ips.RequestProcessing(checkprocess);
                }
            }
            bool ok = true;
            missingprocesstext = "MISSING: ";
            foreach (KeyValuePair<string,double>kvp in missing)
            {
                if (kvp.Value > 0) {
                    ok = false;missingprocesses = true;
                    missingprocesstext += "[" + kvp.Key + " " + kvp.Value + "]";
                }
            }
            MarkDirty();
            return ok;
        }
        public void ToggleAmbientSounds(bool on)
        {
            if (Api.Side != EnumAppSide.Client) return;
            if (runsound == "" || SoundLevel == 0) { return; }
            if (on)
            {

                if (ambientSound == null || !ambientSound.IsPlaying && (!alreadyPlayedSound || loopsound))
                {
                    ambientSound = ((IClientWorldAccessor)Api.World).LoadSound(new SoundParams()
                    {
                        Location = new AssetLocation(runsound),
                        ShouldLoop = loopsound,
                        Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                        DisposeOnFinish = false,
                        Volume = SoundLevel,
                        Range = 10
                    });
                    
                    ambientSound.Start();
                    alreadyPlayedSound = true;
                }
            }
           
            else
            {
                ambientSound?.Stop();
                ambientSound?.Dispose();
                ambientSound = null;
                alreadyPlayedSound = false;
            }
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("missingprocesses", missingprocesses);
            tree.SetString("missingprocesstext", missingprocesstext);
            tree.SetBool("inUse", inUse);
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            missingprocesstext = tree.GetString("missingprocesstext");
            missingprocesses = tree.GetBool("missingprocesses");
            inUse = tree.GetBool("inUse");
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if (missingprocesses) { dsc.Append(missingprocesstext); }
            if (inUse) { dsc.Append("In Use"); }
        }
        
        public override void OnBlockRemoved()
        {
            ambientSound?.Stop();
            ambientSound?.Dispose();
            ambientSound = null;
            base.OnBlockRemoved();
        }
        public override void OnBlockUnloaded()
        {
            ambientSound?.Stop();
            ambientSound?.Dispose();
            ambientSound = null;
            base.OnBlockUnloaded();
        }
        protected BlockEntityAnimationUtil animUtil
        {
            get
            {
                if (GetBehavior<BEBehaviorAnimatable>() == null) { return null; }
                return GetBehavior<BEBehaviorAnimatable>().animUtil;
            }
        }
        bool animationIsRunning = false;
        protected virtual void DoAnimations()
        {
            
            if (ShouldAnimate && !animationIsRunning)
            {

                var meta = new AnimationMetaData() { Animation = animation, Code = animationCode, AnimationSpeed = runAnimationSpeed, EaseInSpeed = 1, EaseOutSpeed = 2, Weight = 1, BlendMode = EnumAnimationBlendMode.Add };
                animUtil.StartAnimation(meta);
                animUtil.StartAnimation(new AnimationMetaData() { Animation = animation, Code = animationCode, AnimationSpeed = 1, EaseInSpeed = 1, EaseOutSpeed = 1, Weight = 1, BlendMode = EnumAnimationBlendMode.Average });
                animationIsRunning = true;
            }
            else if (!ShouldAnimate && animationIsRunning)
            {
                animationIsRunning = false;

                animUtil.StopAnimation(animationCode);
            }

        }
    }
}
