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

namespace qptech.src
{
    //Device to use up electricity
    //intermediate class, shouldn't generally be used
    public class BEEBaseDevice:BEElectric,IConduit
    {
        
        public enum enDeviceState { IDLE, RUNNING, WARMUP, MATERIALHOLD, ERROR }
       
        protected int requiredFlux = 1;     //how much TF to run
        protected int processingTicks = 30; //how many ticks for process to run
        protected int tickCounter = 0;
        protected string animationName = "process";
        public int RequiredFlux { get { return requiredFlux; } }
        //public bool IsPowered { get { return capacitor >= requiredFlux; } }

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
            if (DeviceState != enDeviceState.RUNNING) { return 0; }
            
            return base.ReceivePowerOffer(amt);
        }
        public override void OnTick(float par)
        {
            base.OnTick(par);
            if (deviceState == enDeviceState.RUNNING) { DoRunningParticles(); }
            UsePower();
        }
        protected bool animInit = false;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            animInit = false;
            //if (Block == null || Block.Attributes == null) { return; }
            if (Block.Attributes != null) {
                requiredFlux = Block.Attributes["requiredFlux"].AsInt(requiredFlux);
                processingTicks = Block.Attributes["processingTicks"].AsInt(processingTicks);
                animationName = Block.Attributes["animationName"].AsString(animationName);
            }
            distributionFaces = new List<BlockFacing>(); //no distribution for us!
        }

        protected virtual void DoRunningParticles()
        {

        }
        protected virtual void UsePower()
        {
            if (!isOn) { return; }
            if (DeviceState == enDeviceState.IDLE||DeviceState==enDeviceState.MATERIALHOLD)
            {
                DoDeviceStart();
                
            }
            else if (deviceState == enDeviceState.WARMUP)
            {
                tickCounter++;
                if (tickCounter == 10) { tickCounter = 0;deviceState = enDeviceState.IDLE; }
            }
            else { DoDeviceProcessing(); }
        }

        protected virtual void DoDeviceStart()
        {

            if (Api.World.Side is EnumAppSide.Client) { return; }
            if (!IsPowered) { DoFailedStart(); return; }
            tickCounter = 0;
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
            if (tickCounter >= processingTicks)
            {
                DoDeviceComplete();
                return;
            }
            if (!IsPowered)
            {
                DoFailedProcessing();
                return;
            }
            tickCounter++;
            
            
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
            tickCounter = tree.GetInt("tickCounter");
            deviceState = (enDeviceState)tree.GetInt("deviceState");
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("tickCounter", tickCounter);
            tree.SetInt("deviceState", (int)deviceState);
        }
        protected BlockEntityAnimationUtil animUtil
        {
            get
            {
                BEBehaviorAnimatable bea = GetBehavior<BEBehaviorAnimatable>();
                if (bea == null) { return null; }
                return GetBehavior<BEBehaviorAnimatable>().animUtil;
            }
        }
        
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            dsc.AppendLine("Stat :" + DeviceState.ToString());
            
        }

        public virtual int ReceiveItemOffer(ItemSlot offerslot, BlockFacing onFace)
        {
            return 0;
        }
    }

    public interface IConduit
    {
        int ReceiveItemOffer(ItemSlot offerslot, BlockFacing onFace);
        
    }
}
