using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using qptech.src.networks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace qptech.src
{
    class BEEProcessingSupplier:BEEBaseDevice,IProcessingSupplier
    {
        public Dictionary<string, double> processes;
        double suspendCoolDown = 1000; //how many ms after last use until stopping animations
        double lastUseAt = 0; //last time it was used
        double idleAfter => lastUseAt + suspendCoolDown;
        bool inUse;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (Block.Attributes != null)
            {
                processes = new Dictionary<string, double>();
                processes = Block.Attributes["processes"].AsObject<Dictionary<string,double>>();
                

            }
        }

        protected override bool shouldAnimate => inUse && IsOn&&(deviceState == enDeviceState.RUNNING) && animation != "";
        public override void OnTick(float par)
        {
            if (Api is ICoreServerAPI&&  Api.World.ElapsedMilliseconds > idleAfter) { inUse = false;MarkDirty(); }
            base.OnTick(par);
        }

        public bool RequestProcessing(string process, double strength)
        {
            if (processes == null) { return false; }
            if (!isOn || !IsPowered) { return false; }
            if (deviceState == enDeviceState.POWERHOLD) { return false; }
            if (!processes.ContainsKey(process)) { return false; }
            if (processes[process] < strength) { return false; }
            lastUseAt = Api.World.ElapsedMilliseconds;
            inUse = true;
            return true;
        }
        public double RequestProcessing(string process)
        {
            if (processes == null) { return 0; }
            if (!isOn || !IsPowered) { return 0; }
            if (deviceState == enDeviceState.POWERHOLD) { return 0; }
            if (!processes.ContainsKey(process)) { return 0; }
            lastUseAt = Api.World.ElapsedMilliseconds;
            inUse = true;
            return processes[process];
        }
        public bool CheckProcessing(string process)
        {
            if (processes == null) { return false; }
            if (processes.ContainsKey(process)) { return true; }
            return false;
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("inUse", inUse);
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            inUse = tree.GetBool("inUse");
        }
        
        protected override void UsePower()
        {
            base.UsePower();
            if (IsPowered&&CheckProcessing("heating"))
            {
                
                foreach (BlockFacing bf in BlockFacing.ALLFACES)
                {
                    BlockPos checkpos = Pos.Copy().Offset(bf);
                    IBlockEntityContainer container = Api.World.BlockAccessor.GetBlockEntity(checkpos) as IBlockEntityContainer;
                    if (container == null) { continue; }
                    if (container.Inventory == null) { continue; }
                    if (container.Inventory.Empty) { continue; }
                    foreach (ItemSlot slot in container.Inventory)
                    {
                        if (slot == null || slot.Empty) { continue; }
                        if (slot.Itemstack!=null&&slot.Itemstack.StackSize>0)
                        {
                            float temp = slot.Itemstack.Collectible.GetTemperature(Api.World, slot.Itemstack);
                            double maxHeat=RequestProcessing("heating");
                            if (temp < maxHeat)
                            {
                                float tempGain = (float)(maxHeat-temp)/(500f*(float)slot.Itemstack.StackSize);
                                float newtemp = Math.Min(temp + tempGain,(float)maxHeat);
                                slot.Itemstack.Collectible.SetTemperature(Api.World, slot.Itemstack, newtemp);
                            }
                        }
                    }
                }
            }
        }
    }
}
