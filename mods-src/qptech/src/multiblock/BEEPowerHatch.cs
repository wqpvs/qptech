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

namespace qptech.src.multiblock
{
    class BEEPowerHatch : BEElectric, IFunctionalMultiblockPart
    {
        /// <summary>
        /// TODO - update tree!
        /// </summary>
        IFunctionalMultiblockMaster master;
        public IFunctionalMultiblockMaster Master { get { return master; } set { master = value; } }
        
        public override int ReceivePowerOffer(int amt)
        {
            return base.ReceivePowerOffer(amt);
        }
        public override int RequestPower()
        {
            return base.RequestPower();
        }
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            
        }
        public void OnPartTick(float f)
        {
            //relying on the master to only call this if the MB is functional
            
        }
        public override void FindConnections()
        {
            
            //TODO Add special output connections - maybe go thru the multiblock list and offer to add each member as connection
            //will have to add this functionality to IElectricty (direct power offer or something?)
        }

        public void InitializePart(IFunctionalMultiblockMaster master)
        {
            this.master = master;
        }
    }
}
