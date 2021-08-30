using System;
using System.Linq;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace qptech.src.multiblock

{
    class MBItemHatch : BlockEntityGenericTypedContainer, IFunctionalMultiblockPart
    {
        IFunctionalMultiblockMaster master;
        public IFunctionalMultiblockMaster Master { get { return master; } set { master = value; } }
        bool isOutput = false;
        public bool IsOutput => isOutput;
        public void InitializePart(IFunctionalMultiblockMaster master)
        {
            this.master = master;
            //TODO Figure out if an input or output hatch
        }

        public void OnPartTick(float f)
        {
            
        }

        
    }
}
