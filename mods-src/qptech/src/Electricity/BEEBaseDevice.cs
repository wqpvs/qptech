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
using Vintagestory.API.Client;

namespace qptech.src
{
    //Device to use up electricity
    //intermediate class, shouldn't generally be used
    public class BEEBaseDevice:BEElectric,IConduit
    {
        
        public enum enDeviceState { IDLE, RUNNING, WARMUP, MATERIALHOLD, ERROR, POWERHOLD,PROCESSHOLD }
        ILoadedSound ambientSound;
        string runsound = "";
        protected int requiredFlux = 1;     //how much TF to run
        protected int processingTime = 1000; //how many ticks for process to run
        
        protected string animationCode = "";
        protected string animation = "";
        protected float runAnimationSpeed = 1;
        protected double completetime;
        protected double starttime;
        public int RequiredFlux { get { return requiredFlux; } }
        //public bool IsPowered { get { return capacitor >= requiredFlux; } }
        float soundlevel = 0f;
        bool alreadyPlayedSound = false;
        bool loopsound = false;
        int soundoffdelaycounter = 0;
        
        public virtual float SoundLevel
        {
            get { return soundlevel; }
        }
        protected enDeviceState deviceState = enDeviceState.WARMUP;
        public enDeviceState DeviceState { get { return deviceState; } }
        public override int RequestPower()
        {
            if (!isOn) { return 0; }
            return usePower;
        }
        public override int ReceivePowerOffer(int amt)
        {
            lastPower = Math.Min(amt, usePower); MarkDirty(true);
            if (!IsOn||DeviceState != enDeviceState.RUNNING) { return 0; }
            
            return lastPower;
        }
        public override void OnTick(float par)
        {
            base.OnTick(par);
            if (deviceState == enDeviceState.RUNNING) {
                DoRunningParticles();
                
            }
            ToggleAmbientSounds(shouldAnimate);
            if (Api is ICoreClientAPI) {
                DoAnimations();
                return;
            }
            UsePower();
            
        }
        
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            starttime = 0;
            completetime = 0;
            if (Block.Attributes != null) {
                requiredFlux = Block.Attributes["requiredFlux"].AsInt(requiredFlux);
                processingTime = Block.Attributes["processingTime"].AsInt(processingTime);
                animationCode = Block.Attributes["animationCode"].AsString(animationCode);
                animation = Block.Attributes["animation"].AsString(animation);
                runAnimationSpeed = Block.Attributes["runAnimationSpeed"].AsFloat(runAnimationSpeed);
                runsound = Block.Attributes["runsound"].AsString(runsound);
                soundlevel = Block.Attributes["soundlevel"].AsFloat(soundlevel);
                loopsound = Block.Attributes["loopsound"].AsBool(loopsound);
            }
            if (api.World.Side == EnumAppSide.Client && animationCode != "")
            {
                float rotY = Block.Shape.rotateY;
                animUtil.InitializeAnimator(this.ToString(), new Vec3f(0, rotY, 0));
            }
            
        }

        protected virtual void DoRunningParticles()
        {

        }
        protected virtual void UsePower()
        {
            if (!IsOn) { return; }
            else if (lastPower<usePower && DeviceState == enDeviceState.RUNNING)
            {
                deviceState = enDeviceState.POWERHOLD;
                MarkDirty();
            }
            else if (lastPower>=usePower && DeviceState == enDeviceState.POWERHOLD)
            {
                deviceState = enDeviceState.RUNNING;
                MarkDirty();
            }
            else if (DeviceState == enDeviceState.IDLE||DeviceState==enDeviceState.MATERIALHOLD)
            {
                DoDeviceStart();
                
            }
            
            else if (DeviceState == enDeviceState.RUNNING) { DoDeviceProcessing(); } 
            if (DeviceState == enDeviceState.WARMUP) { deviceState = enDeviceState.IDLE; }
        }
        protected virtual void ResetTimers()
        {
            completetime = Api.World.ElapsedMilliseconds + processingTime;
            starttime = Api.World.ElapsedMilliseconds;
        }
        protected virtual void DoDeviceStart()
        {

            if (Api.World.Side is EnumAppSide.Client) { return; }
            if (!IsPowered) { DoFailedStart(); return; }
            ResetTimers();
            
            if (deviceState == enDeviceState.IDLE)
            {
                
               if (IsPowered)
                {
               
                    deviceState = enDeviceState.RUNNING;
                }
                
            }
            this.MarkDirty(true);

        }

        protected virtual void DoDeviceProcessing()
        {
            if (Api.World.ElapsedMilliseconds>=completetime)
            {
                DoDeviceComplete();
                return;
            }
            if (!IsPowered)
            {
                DoFailedProcessing();
                return;
            }
            
            
            
        }
        //can do some feedback if device can't run
        protected virtual void DoFailedStart()
        {
            deviceState = enDeviceState.IDLE;
        }
        //feedback if device cannot process
        protected virtual void DoFailedProcessing()
        {
            
        }
        //Do whatever needs doing on a successful cycle
        protected virtual void DoDeviceComplete()
        {
            deviceState = enDeviceState.IDLE;
            
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            
            deviceState = (enDeviceState)tree.GetInt("deviceState");
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            
            tree.SetInt("deviceState", (int)deviceState);
        }
        protected BlockEntityAnimationUtil animUtil
        {
            get
            {
                if (GetBehavior<BEBehaviorAnimatable>() == null) { return null; }
                return GetBehavior<BEBehaviorAnimatable>().animUtil;
            }
        }
        protected bool animationIsRunning = false;
        protected virtual bool shouldAnimate => (IsOn&&deviceState == enDeviceState.RUNNING) && animation != "";
        protected virtual void DoAnimations()
        {
            
            if (shouldAnimate && !animationIsRunning)
            {

                var meta = new AnimationMetaData() { Animation = animation, Code = animationCode, AnimationSpeed = runAnimationSpeed, EaseInSpeed = 1, EaseOutSpeed = 2, Weight = 1, BlendMode = EnumAnimationBlendMode.Add };
                animUtil.StartAnimation(meta);
                animUtil.StartAnimation(new AnimationMetaData() { Animation = animation, Code = animationCode, AnimationSpeed = runAnimationSpeed, EaseInSpeed = 1, EaseOutSpeed = 1, Weight = 1, BlendMode = EnumAnimationBlendMode.Average });
                animationIsRunning = true;
            }
            else if (!shouldAnimate && animationIsRunning)
            {
                animationIsRunning = false;

                animUtil.StopAnimation(animationCode);
            }

        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            dsc.Append(" "+DeviceState.ToString());
            dsc.AppendLine("");
            
        }

        public virtual int ReceiveItemOffer(ItemSlot offerslot, BlockFacing onFace)
        {
            return 0;
        }
        public override void CleanBlock()
        {
            ambientSound?.Stop();
            ambientSound?.Dispose();
            ambientSound = null;
            base.CleanBlock();
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
        public void ToggleAmbientSounds(bool on)
        {
            if (Api.Side != EnumAppSide.Client) return;
            if (runsound == "" || SoundLevel == 0) { return; }
            if (on)
            {
                
                if (ambientSound == null || !ambientSound.IsPlaying && (!alreadyPlayedSound||loopsound))
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
                    soundoffdelaycounter = 0;
                    ambientSound.Start();
                    alreadyPlayedSound = true;
                }
            }
            else if (loopsound && soundoffdelaycounter<10)
            {
                soundoffdelaycounter++;
            }
            else 
            {
                ambientSound?.Stop();
                ambientSound?.Dispose();
                ambientSound = null;
                alreadyPlayedSound = false;
            }

        }
        public override string GetStatusUI()
        {
            return base.GetStatusUI();
        }
    }
   
    public interface IConduit
    {
        int ReceiveItemOffer(ItemSlot offerslot, BlockFacing onFace);
        
    }
}
