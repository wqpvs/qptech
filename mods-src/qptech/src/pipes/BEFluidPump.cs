using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using qptech.src.networks;


namespace qptech.src
{
    class BEFluidPump : BEFluidPipe
    {
        BlockFacing pumpin = BlockFacing.DOWN;
        BlockFacing pumpout = BlockFacing.UP;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (Block.Attributes != null)
            {
                pumpin = BEElectric.OrientFace(Block.Code.ToString(), BlockFacing.FromCode( Block.Attributes["pumpin"].AsString("down")));
                pumpout = BEElectric.OrientFace(Block.Code.ToString(), BlockFacing.FromCode(Block.Attributes["pumpout"].AsString("up")));
                checkfaces = new BlockFacing[2];
                checkfaces[0] = pumpin;
                checkfaces[1] = pumpout;

            }
        }
        public override Guid GetNetworkID(BlockPos requestedby, string fortype)
        {

            BlockPos checkin = Pos.AddCopy(pumpin);
            BlockPos checkout = Pos.AddCopy(pumpout);
            if (requestedby == checkin || requestedby == checkout)
            {
                return NetworkID;
            }

            
            return Guid.Empty;

        }

        public override void DoConnections()
        {

            inputNodes = new List<BlockEntity>();
            outputNodes = new List<BlockEntity>();
            
            //Check for tanks below and beside and fill appropriately
            foreach (BlockFacing bf in checkfaces) //used facechecker to make sure down is processed first
            {

                if (disabledFaces.Contains(bf)) { continue; }
                IFluidNetworkMember fnm = Api.World.BlockAccessor.GetBlockEntity(Pos.Copy().Offset(bf)) as IFluidNetworkMember;
                if (fnm != null)
                {

                    continue;
                }

                ///HERE IS WHERE WE'LL PUT CODE TO FILL IN OUTPUT NODE INFO
                ///
                BlockEntity finde = Api.World.BlockAccessor.GetBlockEntity(Pos.Copy().Offset(bf)) as BlockEntity;

                if (finde == null) { continue; }              //is it a container?
                IFluidTank bt = finde as IFluidTank;
                IFluidNetworkUser fnu = finde as IFluidNetworkUser;
                if (bt == null && fnu == null) { continue; }
                
                if (bf == pumpout)
                {
                    outputNodes.Add(finde);
                }
                else
                {
                    inputNodes.Add(finde);
                }

            }

        }
    }
}
