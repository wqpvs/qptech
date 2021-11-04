using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qptech.src.multiblock
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.IO;
    using Vintagestory.API.Common;
    using Vintagestory.API.Datastructures;
    using Vintagestory.API.MathTools;
    using Vintagestory.GameContent;
    using Vintagestory.API.Server;
    using Vintagestory.API;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common.Entities;
    using Vintagestory.API.Config;
    using Vintagestory.API.Util;
    using Vintagestory.ServerMods;

    class BEEMBHeater : BEElectric, IFunctionalMultiblockPart
    {
        int fluxPerTick = 5;
        float heatPerTickPerLiter = 700;

        IFunctionalMultiblockMaster master;
        public IFunctionalMultiblockMaster Master { get { return master; } set { master = value; } }

        public void OnPartTick(float f)
        {
            if (!IsPowered || !IsOn || master == null) { return; }
            
            IMultiblockHeatUser masterheat = master as IMultiblockHeatUser;
            if (masterheat != null )
            {
                masterheat.ReceiveHeat(heatPerTickPerLiter);
                
            }
            //TODO IN FUTURE - check each part for heat usage?
        }
        public void InitializePart(IFunctionalMultiblockMaster master)
        {
            this.master = master;
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            
        }
    }
}
