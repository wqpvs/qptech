using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.API.Util;

namespace qptech.src.misc
{
    class MultiBlockTest:BlockEntity
    {
        ICoreAPI capi;
        MultiblockStructure ms;
        public BlockPos mboffset;
        public override void Initialize(ICoreAPI api)
        {
            capi = api as ICoreClientAPI;
            ms = Block.Attributes["multiblockStructure"].AsObject<MultiblockStructure>();
            mboffset = Pos.Copy().Offset(BlockFacing.WEST).Copy().Offset(BlockFacing.DOWN);
            ms.InitForUse(0);
            
            base.Initialize(api);
        }
        
        bool showingcontents = false;
        public bool Interact(IPlayer byPlayer)
        {
            showingcontents = !showingcontents;
            if(Api.Side == EnumAppSide.Client)
            {
                if (showingcontents)
                {
                    ms.HighlightIncompleteParts(Api.World, byPlayer, mboffset);
                }
                else
                {
                    ms.ClearHighlights(Api.World, byPlayer);
                }
            }
            return true;
        }
    }

    public class BlockMBT: Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockPos pos = blockSel.Position;
           

            MultiBlockTest besc = world.BlockAccessor.GetBlockEntity(pos) as MultiBlockTest;
            if (besc != null)
            {
                besc.Interact(byPlayer);
                (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                return true;
            }

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

    }

    public class MBTLoader : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterBlockEntityClass("MultiBlockTest", typeof(MultiBlockTest));
            api.RegisterBlockClass("BlockMBT", typeof(BlockMBT));
        }
    }
}
