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
using qptech.src.networks;

namespace qptech.src.networks
{
    class BEProcessToProcess : BlockEntity, IProcessingSupplier
    {
        public Dictionary<string, double> suppliedProcesses;
        public Dictionary<string, double> requiredProcesses;
        Dictionary<string, double> missing;
        bool missingprocesses = false;
        string missingprocesstext = "";
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (Block.Attributes != null)
            {
                suppliedProcesses = new Dictionary<string, double>();
                suppliedProcesses = Block.Attributes["processes"].AsObject<Dictionary<string, double>>();
                requiredProcesses = new Dictionary<string, double>();
                requiredProcesses = Block.Attributes["requiredProcesses"].AsObject<Dictionary<string, double>>();
                missing = new Dictionary<string, double>();
            }
        }

        public bool RequestProcessing(string process, double strength)
        {
            if (suppliedProcesses == null||requiredProcesses==null) { return false; }
            
            if (!suppliedProcesses.ContainsKey(process)) { return false; }
            if (!CheckRequiredProcesses()) { return false; }
            if (suppliedProcesses[process] < strength) { return false; }
            return true;
        }

        public double RequestProcessing(string process)
        {
            if (suppliedProcesses == null || requiredProcesses == null) { return 0; }
            if (!suppliedProcesses.ContainsKey(process)) { return 0; }
            if (!CheckRequiredProcesses()) { return 0; }
            return suppliedProcesses[process];
        }
        
        protected virtual bool CheckRequiredProcesses()
        {
            missing = new Dictionary<string, double>(requiredProcesses);
            missingprocesses = false;
            if (missing.Count == 0) { MarkDirty(); return true; }
            BlockFacing[] checkfaces = BlockFacing.ALLFACES;
            foreach (BlockFacing bf in checkfaces)
            {
                
                IProcessingSupplier ips = Api.World.BlockAccessor.GetBlockEntity(Pos.Copy().Offset(bf)) as IProcessingSupplier;
                if (ips == null) { continue; }
                foreach (string checkprocess in missing.Keys)
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
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("missingprocesses", missingprocesses);
            tree.SetString("missingprocesstext", missingprocesstext);
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            missingprocesstext = tree.GetString("missingprocesstext");
            missingprocesses = tree.GetBool("missingprocesses");
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if (missingprocesses) { dsc.Append(missingprocesstext); }
        }
    }
}
