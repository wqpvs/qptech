using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;

namespace qptech.src.networks
{
    class FluidNetwork : IFlexNetwork
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
            if (member==null) { return false; }
            IFluidNetworkMember fnm = member as IFluidNetworkMember;
            if (fnm == null) { return false; }
            if (fluid == "" && fnm.Fluid != "") { fluid = fnm.Fluid; }
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
            Dictionary<int, List<IFluidNetworkMember>> heightindex = new Dictionary<int, List<IFluidNetworkMember>>();

            foreach (IFlexNetworkMember fn in GetMembers().ToArray())
            {
                IFluidNetworkMember flum = fn as IFluidNetworkMember;
                if (flum == null) { continue; }
                networkCapacity += flum.GetFluidTotalCapacity();
                int height = flum.GetHeight();
                if (!heightindex.ContainsKey(height))
                {
                    heightindex[height] = new List<IFluidNetworkMember>();
                }
                heightindex[height].Add(flum);
                networkLevel += flum.GetFluidLevel();
                fnmlist.Add(flum);
            }
            if (networkLevel==0||networkCapacity==0) { FlexNetworkManager.DeleteNetwork(NetworkID);return; }
            var membersByHeight = fnmlist.OrderBy(m => m.GetHeight());
            int fluidcounter = (int)networkLevel;
            foreach (int h in heightindex.Keys.OrderBy(x => x))
            {
                int heightpop = heightindex[h].Count();
                int avgfluid = fluidcounter / heightpop;
                foreach (IFluidNetworkMember fnm in heightindex[h])
                {
                    fluidcounter -= fnm.SetFluidLevel(avgfluid, Fluid);
                    if (fluidcounter <= 0) { break; }
                }
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
        
        int GetHeight();
        int SetFluidLevel(int amt,string fluid); //This would factor in how much fluid a pipe could actualy transfer
    }
}
