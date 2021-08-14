using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace qptech.src
{
    class BlockEForge:Block,IBlockItemFlow
    {
        public string[] PullFaces => Attributes["pullFaces"].AsArray<string>(new string[0]);
        public string[] PushFaces => Attributes["pushFaces"].AsArray<string>(new string[0]);
        public string[] AcceptFaces => Attributes["acceptFromFaces"].AsArray<string>(new string[0]);
        public bool HasItemFlowConnectorAt(BlockFacing facing)
        {
            return PullFaces.Contains(BEElectric.OrientFace(Code.ToString(),facing).ToString()) || PushFaces.Contains(BEElectric.OrientFace(Code.ToString(), facing).ToString()) || AcceptFaces.Contains(BEElectric.OrientFace(Code.ToString(), facing).ToString());
        }
        
        //WorldInteraction[] interactions;
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BEEForge bea = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEEForge;
            if (bea != null)
            {
                return bea.OnPlayerInteract(world, byPlayer, blockSel);
            }

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        
    }
}
