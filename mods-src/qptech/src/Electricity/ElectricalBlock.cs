using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;
using Vintagestory.API.Client;

namespace qptech.src
{
    class ElectricalBlock : BlockMPBase,IBlockItemFlow
    {
        public virtual string[] PullFaces => Attributes["pullFaces"].AsArray<string>(new string[0]);
        public virtual string[] PushFaces => Attributes["pushFaces"].AsArray<string>(new string[0]);
        public virtual string[] AcceptFaces => Attributes["acceptFromFaces"].AsArray<string>(new string[0]);

        public virtual bool HasItemFlowConnectorAt(BlockFacing facing)
        {
            return PullFaces.Contains(BEElectric.OrientFace(Code.ToString(), facing).ToString()) || PushFaces.Contains(BEElectric.OrientFace(Code.ToString(), facing).ToString()) || AcceptFaces.Contains(BEElectric.OrientFace(Code.ToString(), facing).ToString());
        }
        long powertogglecooldown = 1200;
        long nextpowertoggleat = 0;
        public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
        {

        }

        public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
        {
            return false;
        }

        //Toggle power if player is holding a screwdriver or club
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            
            var bee = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEElectric;
            
            if (bee != null && byPlayer.Entity.RightHandItemSlot.Itemstack == null)
            {
                bee.OpenStatusGUI();
            }
            if (bee==null) return base.OnBlockInteractStart(world, byPlayer, blockSel); 
            if (byPlayer.Entity.RightHandItemSlot.Itemstack==null) return base.OnBlockInteractStart(world, byPlayer, blockSel);
            if (byPlayer.Entity.RightHandItemSlot.Itemstack.Item == null) return base.OnBlockInteractStart(world, byPlayer, blockSel);
            string fcp = byPlayer.Entity.RightHandItemSlot.Itemstack.Item.CodeWithoutParts(1);
            if ((fcp.Contains("screwdriver")&&!fcp.Contains("head"))||fcp.Contains("woodenclub"))
            {
               // if (world.ElapsedMilliseconds > nextpowertoggleat)
               // {
                    bee.TogglePower();
                   // nextpowertoggleat = world.ElapsedMilliseconds + powertogglecooldown;
               // }
                return true;
            }
            else if (byPlayer.Entity.RightHandItemSlot.Itemstack.Item.Code.ToString().Contains("wrench"))
            {
                bee.Wrench();
            }
            else if (byPlayer.Entity.RightHandItemSlot.Itemstack.Item.Code.ToString().StartsWith("machines:cable-"))
            {
                return bee.OnWireClick(world, byPlayer, blockSel);
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            BEElectric bee = world.BlockAccessor.GetBlockEntity(pos) as BEElectric;
            if (bee != null)
            {
                bee.FindConnections();
            }
            base.OnNeighbourBlockChange(world, pos, neibpos);
        }
    }
}
