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
    class BEBMultiDummy : BlockEntityBehavior, IDummyParent
    {
        public string dummyblockname => "machines:dummy";
        List<BEDummyBlock> dummies;
        int[] dummylocations;
        BlockEntity be;
        long l1;
        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            if (Api is ICoreServerAPI)
            {
                InstantiateDummies();
                
            }
            l1 = be.RegisterDelayedCallback(SetupDummies, 1);
        }

        protected virtual void InstantiateDummies()
        {
            if (be.Block.Attributes != null)
            {
                int[] dummylocations = be.Block.Attributes["dummylocations"].AsArray<int>();
                if (dummylocations != null && dummylocations.Length > 0 && Api is ICoreServerAPI)
                {
                    dummies = new List<BEDummyBlock>();
                    Block dummyblock = Api.World.BlockAccessor.GetBlock(new AssetLocation(dummyblockname));

                    for (int c = 0; c < dummylocations.Length; c += 3)
                    {
                        BlockPos dpos = new BlockPos(be.Pos.X + dummylocations[c], be.Pos.Y + dummylocations[c + 1], be.Pos.Z + dummylocations[c + 2]);
                        Api.World.BlockAccessor.SetBlock(dummyblock.BlockId, dpos);
                        
                    }
                }
            }
        }

        public virtual void SetupDummies(float dt)
        {
            if (be.Block.Attributes != null)
            {
                int[] dummylocations = be.Block.Attributes["dummylocations"].AsArray<int>();
                if (dummylocations != null && dummylocations.Length > 0 && Api is ICoreServerAPI)
                {
                    dummies = new List<BEDummyBlock>();
                    

                    for (int c = 0; c < dummylocations.Length; c += 3)
                    {
                        BlockPos dpos = new BlockPos(be.Pos.X+dummylocations[c], be.Pos.Y + dummylocations[c + 1], be.Pos.Z + dummylocations[c + 2]);
                        //adjust for facing
                        BEDummyBlock bed = Api.World.BlockAccessor.GetBlockEntity(dpos) as BEDummyBlock;
                        
                        if (bed != null)
                        {
                            bed.SetParent(this);
                            dummies.Add(bed);
                        }
                    }
                }
            }
        }
        public BEBMultiDummy(BlockEntity be) : base(be)
        {
            this.be = be;

        }
        public void OnDummyBroken()
        {
            
            Api.World.BlockAccessor.BreakBlock(be.Pos, null);
        }

        public string GetDisplayName()
        {
            return be.Block.GetPlacedBlockName(Api.World,be.Pos);
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

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            
        }
    }
    
}
