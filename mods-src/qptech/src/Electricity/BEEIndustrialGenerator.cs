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

namespace qptech.src
{
    /// <summary>
    /// this class will deprecate the old generator class eventually
    /// </summary>
    class BEEIndustrialGenerator:BEElectric
    {
        Dictionary<string, double> requiredProcesses;
        Dictionary<string, double> missing;
        List<BlockFacing> processInputFaces;
        bool missingprocesses = false;
        string missingprocesstext = "";
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (Block.Attributes != null)
            {
                requiredProcesses = new Dictionary<string, double>();
                requiredProcesses = Block.Attributes["requiredProcesses"].AsObject<Dictionary<string, double>>();
                missing = new Dictionary<string, double>();
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
            }
        }

        
        public override int AvailablePower()
        {
            if (!CheckRequiredProcesses()) { return 0; }
            return base.AvailablePower();
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
            foreach (KeyValuePair<string, double> kvp in missing)
            {
                if (kvp.Value > 0)
                {
                    ok = false; missingprocesses = true;
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
