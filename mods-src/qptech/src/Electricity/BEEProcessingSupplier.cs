using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using qptech.src.networks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace qptech.src
{
    class BEEProcessingSupplier:BEEBaseDevice,IProcessingSupplier
    {
        public Dictionary<string, double> processes;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (Block.Attributes != null)
            {
                processes = new Dictionary<string, double>();
                processes = Block.Attributes["processes"].AsObject<Dictionary<string,double>>();
                

            }
        }
        public bool RequestProcessing(string process, double strength)
        {
            if (processes == null) { return false; }
            if (!isOn || !IsPowered) { return false; }
            if (!processes.ContainsKey(process)) { return false; }
            if (processes[process] < strength) { return false; }
            return true;
        }
        public double RequestProcessing(string process)
        {
            if (processes == null) { return 0; }
            if (!isOn || !IsPowered) { return 0; }
            if (!processes.ContainsKey(process)) { return 0; }
            return processes[process];
        }
    }
}
