using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.API.Client;
using qptech.src.networks;


namespace qptech.src.multiblock
{
    class BEBMultiDummy:BlockEntityBehavior,IDummyParent
    {
        public string dummyblockname => "machines:dummy";
        List<BEDummyBlock> dummies;
        int[] dummylocations;
        BlockEntity be;
        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            if (be.Block.Attributes != null)
            {
                int[] dummylocations = be.Block.Attributes["dummylocations"].AsArray<int>();
                if (dummylocations != null && dummylocations.Length > 0 && api is ICoreServerAPI)
                {
                    dummies = new List<BEDummyBlock>();
                    Block dummyblock = Api.World.BlockAccessor.GetBlock(new AssetLocation(dummyblockname));

                    for (int c = 0; c < dummylocations.Length; c += 3)
                    {
                        BlockPos dpos = new BlockPos(dummylocations[c], dummylocations[c + 1], dummylocations[c + 2]);
                        //adjust for facing
                        BEDummyBlock bed = Api.World.BlockAccessor.GetBlockEntity(dpos) as BEDummyBlock;
                        if (bed == null)
                        {
                            Api.World.BlockAccessor.SetBlock(dummyblock.BlockId, dpos);
                            //Api.World.BlockAccessor.SpawnBlockEntity("BEDummyBlock", dpos);
                            
                            bed = Api.World.BlockAccessor.GetBlockEntity(dpos) as BEDummyBlock;
                        }
                        bed.parentblock = this;
                        dummies.Add(bed);
                    }
                }
            }
        }
        public BEBMultiDummy(BlockEntity be):base(be)
        {
            this.be = be;
            
        }
        public void OnDummyBroken()
        {
            Api.World.BlockAccessor.SetBlock(0, be.Pos);
        }
        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            base.OnBlockBroken(byPlayer);
            if (dummies != null)
            {
                foreach (BEDummyBlock dummy in dummies)
                {
                    if (dummy != null) { dummy.ParentBroken(); }
                }
            }
        }
    }
}
