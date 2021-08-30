using System;
using System.Linq;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;
using System.Text;

namespace qptech.src.multiblock

{
    class MBItemHatch : BlockEntityGenericTypedContainer, IFunctionalMultiblockPart
    {
        IFunctionalMultiblockMaster master;
        public IFunctionalMultiblockMaster Master { get { return master; } set { master = value; } }
        bool isOutput = false;
       
        public bool IsOutput => isOutput;
        public bool IsInput => !IsOutput;
        public void InitializePart(IFunctionalMultiblockMaster master)
        {
            this.master = master;
            
            //if block output position (North right now...) inside of structre than it is an input hatch
            isOutput = true;
            BlockPos outputpos = Pos.NorthCopy();
            
            foreach (Vec4i v in master.MBStructure.Offsets)
            {
                if (v.X + master.MBOffset.X == outputpos.X && v.Y + master.MBOffset.Y == outputpos.Y && v.Z + master.MBOffset.Z==outputpos.Z)
                {
                    isOutput = false;
                    break;
                }
            }
        }

        public void OnPartTick(float f)
        {
            
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            
            if (master == null || master.MBStructure == null) { return; }
            if (isOutput) { dsc.AppendLine("OUTPUT"); }
            if (IsInput) { dsc.AppendLine("INPUT"); }
            base.GetBlockInfo(forPlayer, dsc);
        }

    }
}
