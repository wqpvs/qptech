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

namespace RustAndRails.src
{
    class BlockRail:Block
    {
        public virtual string GetRailState(IWorldAccessor world, BlockPos pos, MinecartEntity cart)
        {
            var railblock = world.BlockAccessor.GetBlock(pos) as BlockRail;
            
            if (railblock != null)
            {
                //option one check for a static railstate
                if (railblock.Attributes != null)
                {
                    return railblock.Attributes["railstate"].AsString("");
                }
            }
            //option two: (TODO) check for stack attributes
            //option three: (TODO) check for relevant block behaviours and pass query on
            return "";
        }
        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {

            BlockFacing getface = BlockFacing.HorizontalFromAngle(byPlayer.Entity.ServerPos.Yaw);
            Block orientedBlock = itemstack.Block;
            if (getface.IsAxisWE)
            {
                string orgcode = itemstack.Collectible.Code.ToString();
                orgcode = orgcode.Replace("flat_ns", "flat_we");
                orientedBlock = world.GetBlock(new AssetLocation(orgcode));
                if (orientedBlock == null)
                {
                    throw new System.NullReferenceException("Unable to to find a rotated block with code " + orgcode + ", you're maybe missing the side variant group of have a dash in your block code");
                }
            }

            if (orientedBlock.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
            {
                orientedBlock.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
                return true;
            }
            return false;
        }
    }
}
