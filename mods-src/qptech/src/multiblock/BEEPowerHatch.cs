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
using Electricity.API;

namespace qptech.src.multiblock
{
    class BEEPowerHatch : BEElectric, IFunctionalMultiblockPart
    {
        IFunctionalMultiblockMaster master;
        public IFunctionalMultiblockMaster Master { get { return master; } set { master = value; } }

        public void OnPartTick(float f)
        {
            //relying on the master to only call this if the MB is functional
            if (IsOn&&IsPowered&&Capacitor>0)
            {
                IElectricity mastere = master as IElectricity;
                int fluxused = 0;

                //FIRST Offer Power to the master
                if (mastere !=null)
                {
                    int used = mastere.ReceivePacketOffer(this, Math.Min(Capacitor, maxFlux));
                    ChangeCapacitor(-used);
                    fluxused += used;
                }
                //Offer power to each part
                foreach (Block b in master.Parts)
                {
                    int availablepower = Math.Min(Capacitor, MaxFlux - fluxused);
                    if (availablepower < 1) { break; }
                    IElectricity ie = b as IElectricity;
                    if (ie == null) { continue; }
                    int used = ie.ReceivePacketOffer(this, availablepower);
                    ChangeCapacitor(-used);
                    fluxused += used;
                }
            }   
        }
        public override void FindConnections()
        {
            ClearConnections();
            FindInputConnections();
            //TODO Add special output connections - maybe go thru the multiblock list and offer to add each member as connection
            //will have to add this functionality to IElectricty (direct power offer or something?)
        }
    }
}
