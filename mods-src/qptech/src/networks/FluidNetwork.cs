using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.API.Server;

namespace qptech.src.networks
{
    class FluidNetwork : IFlexNetwork
    {
        Guid networkID;
        public Guid NetworkID => networkID;

        public string ProductID => "FLUID";
        public string fluid = "";
        public string Fluid => fluid;
        int networkCapacity;
        int networkLevel;
        public int NetworkCapacity => networkCapacity;
        public int NetworkLevel => networkLevel;
        List<IFlexNetworkMember> members;
        public FluidNetwork(Guid newid)
        {
            members = new List<IFlexNetworkMember>();
            networkID = newid;
        }
        public List<IFlexNetworkMember> GetMembers()
        {
            if (members == null) { members = new List<IFlexNetworkMember>(); }
            return members;
        }

        public bool JoinNetwork(IFlexNetworkMember member)
        {
            if (member == null) { return false; }
            IFluidNetworkMember fnm = member as IFluidNetworkMember;
            if (fnm == null) { return false; }
            
            if (!GetMembers().Contains(fnm))
            {
                GetMembers().Add(fnm);
            }
            return true;
        }

        public bool MergeWith(Guid newnetworkID)
        {
            return true;
        }

        public void OnTick(float dt)
        {
            //Iterate thru members and average out fluid
            //if fluid level is zero then set all fluid to ""
            networkCapacity = 0;
            networkLevel = 0;
            int maxflow = 500;
            if (GetMembers().Count == 0) { FlexNetworkManager.DeleteNetwork(NetworkID); return; }
            List<BlockEntityContainer> validoutputs = new List<BlockEntityContainer>();
            List<BlockEntityContainer> validinputs = new List<BlockEntityContainer>();
            Item fluiditem = null;
            //Do inventory of available fluids, set fluid type
            foreach (IFlexNetworkMember nm in GetMembers().ToArray())
            {
                IFluidNetworkMember fnm = nm as IFluidNetworkMember;
                if (fnm == null) { continue; }
                //Do an inventory of available fluid
                maxflow = Math.Min(maxflow, fnm.FluidRate);
                foreach (BlockEntityContainer inputnode in fnm.InputNodes().ToArray())
                {
                    if (inputnode == null) { continue; }
                    if (inputnode.Inventory == null) { continue; }
                    if (inputnode.Inventory.Empty) { continue; }
                    //handle tanks
                    IFluidTank inputtank = inputnode as IFluidTank;
                    if (inputtank != null)
                    {
                        //assign fluid for this cycle if available
                        if (inputtank.CurrentLevel>0&& inputtank.CurrentItem!=null && fluiditem == null)
                        {
                            fluiditem = inputtank.CurrentItem;
                        }
                        if (!validinputs.Contains(inputnode)&& inputtank.CurrentLevel > 0 && inputtank.CurrentItem == fluiditem)
                        {
                            
                            validinputs.Add(inputnode);
                            networkLevel += inputtank.CurrentLevel;
                        }
                    }
                }
                
                
            }
            //if no fluid is available we won't bother  checking outputs
            if (fluiditem == null || networkLevel == 0) { return; }
            int totalfluidused = 0;
            foreach (IFlexNetworkMember nm in GetMembers().ToArray())
            {
                if (networkLevel <= 0) { break; }
                IFluidNetworkMember fnm = nm as IFluidNetworkMember;
                if (fnm == null) { continue; }
                
                //Do an inventory of available fluid
                foreach (BlockEntityContainer outputnode in fnm.OutputNodes().ToArray())
                {
                    if (networkLevel <= 0) { break; }
                    if (outputnode == null) { continue; }
                    if (validinputs.Contains(outputnode)) { continue; }
                    if (outputnode.Inventory == null) { continue; }
                    
                    //handle tanks
                    IFluidTank outputtank = outputnode as IFluidTank;
                    if (outputtank != null)
                    {
                       if (outputtank.IsFull) { continue; }
                       if (outputtank.CurrentItem != null && outputtank.CurrentItem != fluiditem) { continue; }
                       if (validoutputs.Contains(outputnode)) { continue; }
                        //this all lines up so we could now do inventory transfer
                        //** Need to add a check for tankpos==itself to the fluid tank class!!**
                        int used = outputtank.ReceiveFluidOffer(fluiditem, Math.Min(maxflow,networkLevel), outputtank.TankPos);
                        networkLevel -= used;
                        totalfluidused += used;
                        validoutputs.Add(outputnode);
                    }
                }


            }
            //Finally we need to go through fluid that was used and take from source containers
            foreach (BlockEntityContainer srccont in validinputs)
            {
                if (totalfluidused <= 0) { break; }
                IFluidTank ift = srccont as IFluidTank;
                if (ift == null) { continue; }
                if (ift.CurrentLevel <= 0){ continue; }
                int taken = ift.TryTakeFluid(totalfluidused, ift.TankPos);
                totalfluidused -= taken;
            }
        }

        public void RemoveNetwork()
        {
            foreach (IFlexNetworkMember m in GetMembers())
            {
                if (m != null && m.NetworkID == NetworkID) { m.NetworkRemove(); }

            }
        }
    }
    
    interface IFluidNetworkMember : IFlexNetworkMember
    {
        
        List<BlockEntityContainer> OutputNodes();
        List<BlockEntityContainer> InputNodes();
        int FluidRate { get; }

    }
}
