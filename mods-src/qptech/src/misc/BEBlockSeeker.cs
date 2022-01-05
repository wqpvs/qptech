using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;

namespace qptech.src.misc
{
    class BEBlockSeekerLoader : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterBlockEntityClass("BEBlockSeeker", typeof(BEBlockSeeker));
        }
    }
    class BEBlockSeeker: BlockEntity
    {
        string searchstatus;
        string searchfor = "rock-chert";
        Block searchblock;
        bool seeking = false;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            RegisterGameTickListener(OnTick, 50);
        }
        
        void OnTick(float dt)
        {
            if (!seeking) { StartSearch(); }
        }
        int xzsearch = 10;
        int counter = 0;
        void StartSearch()
        {
            seeking = true;
            AssetLocation seekal = new AssetLocation(searchfor);
            searchblock = Api.World.BlockAccessor.GetBlock(seekal);
            BlockPos startPos = Pos.Copy();
            BlockPos endPos = Pos.Copy();
            startPos.X -= xzsearch;endPos.X += xzsearch;
            startPos.Y = 50; endPos.Y = 200;
            startPos.Z -= xzsearch; startPos.Z += xzsearch;
            searchstatus = "searching for "+searchfor+" " + startPos.ToString() + " to " + endPos.ToString();
            Api.World.BlockAccessor.SearchBlocks(startPos, endPos,onBlock);
            MarkDirty(true);
        }

        public bool onBlock(Block forblock, BlockPos atpos)
        {
            counter++;
            MarkDirty();
            if (forblock != searchblock) { return false; }
            
            atpos = Pos - atpos;
            searchstatus = forblock.ToString() + " found at relative coords " + atpos.ToString();
            
            return true;
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            dsc.AppendLine("STATUS:");
            dsc.AppendLine(searchstatus);
        }

        
    }
}
