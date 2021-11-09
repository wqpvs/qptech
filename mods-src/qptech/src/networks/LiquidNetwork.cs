using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;

namespace qptech.src.networks
{
    class LiquidNetwork : IFlexNetwork
    {
        Guid networkID;
        public Guid NetworkID => networkID;

        public string ProductID => "FLUID";
        public string fluid = "";
        public string Fluid => fluid;
        double networkCapacity;
        double networkLevel;
        public double NetworkCapacity => networkCapacity;
        public double NetworkLevel => networkLevel;
        List<IFlexNetworkMember> members;
        public List<IFlexNetworkMember> GetMembers()
        {
            if (members == null) { members = new List<IFlexNetworkMember>(); }
            return members;
        }

        public bool JoinNetwork(IFlexNetworkMember member)
        {
            if (member==null) { return false; }
            IFluidNetworkMember fnm = member as IFluidNetworkMember;
            if (fnm == null) { return false; }
            if (!fnm.IsEmpty() && fnm.Fluid != Fluid) { return false; }
            if (fnm.IsEmpty()) { fnm.Fluid = Fluid; }
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
            if (GetMembers().Count == 0) { FlexNetworkManager.DeleteNetwork(NetworkID);return; }
            List<IFluidNetworkMember> fnmlist = new List<IFluidNetworkMember>();
            foreach (IFlexNetworkMember fn in GetMembers().ToArray())
            {
                IFluidNetworkMember flum = fn as IFluidNetworkMember;
                if (flum == null) { continue; }
                networkCapacity += flum.GetFluidTotalCapacity();
                
                networkLevel += flum.GetFluidLevel();
                fnmlist.Add(flum);
            }
            if (networkLevel==0||networkCapacity==0) { FlexNetworkManager.DeleteNetwork(NetworkID);return; }
            var membersByHeight = fnmlist.OrderBy(m => m.GetHeight());
            int fluidcounter = (int)networkLevel;
            
            foreach (IFluidNetworkMember flum in membersByHeight)
            {
                int used = flum.SetFluidLevel(fluidcounter);
                fluidcounter -= used;
                if (fluidcounter <= 0) { break; }
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
        string Fluid { get; set; }
        int FluidRate { get; }
        int GetFluidLevel();
        int GetFluidTotalCapacity();
        int GetFluidAvailableCapacity();
        bool IsEmpty();
        int ReceiveFluidOffer(int amt);
        int GetHeight();
        int SetFluidLevel(int amt); //This would factor in how much fluid a pipe could actualy transfer
    }
}
