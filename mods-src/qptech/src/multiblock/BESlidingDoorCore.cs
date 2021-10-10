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
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;


namespace qptech.src.multiblock
{
    class BESlidingDoorCore:BlockEntity,IMasterBlock
    {
        List<ISlaveBlock> slaveblocks;
        public List<ISlaveBlock> SlaveBlocks { get { if (slaveblocks == null) { slaveblocks = new List<ISlaveBlock>(); } return slaveblocks; } set { slaveblocks = value; } }
        int blocksHigh = 2;
        int blocksWide = 2;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (Block.Attributes != null)
            {
                blocksHigh = Block.Attributes["blocksHigh"].AsInt(blocksHigh);
                blocksWide = Block.Attributes["blocksWide"].AsInt(blocksWide);
                SetupBlocks();
            }
        }
        public override void OnBlockBroken()
        {
            
                foreach (ISlaveBlock isb in SlaveBlocks)
                {
                    BlockEntity sbe = isb as BlockEntity;
                    if (sbe == null) { continue; }
                    Api.World.BlockAccessor.BreakBlock(sbe.Pos, null);
                }
            base.OnBlockBroken();
        }

        public void BreakMe()
        {
            Api.World.BlockAccessor.BreakBlock(Pos,null);
        }
        public void SetupBlocks()
        {
            if (Api is ICoreClientAPI) { return; }
            string[] blocklist = Block.Attributes["blocks"].AsArray<string>();
            //if no valid block list will just destroy itself
            if (blocklist == null || blocklist.Length < blocksHigh * blocksWide)
            {
                BreakMe();
            }
            int indexc = 0;
            for (int heightc = 0; heightc < blocksHigh; heightc++)
            {
                for (int widthc = 0; widthc < blocksWide; widthc++)
                {
                    if (heightc == 0 && widthc == 0) { indexc++;continue; } //this should be our position - will add offsets later but skip
                    string blockname = blocklist[indexc];
                    
                    BlockPos chkpos = Pos.Copy();
                    chkpos.Y += heightc;
                    if (Block.Code.ToString().Contains("north"))
                    {
                        chkpos.X += widthc;
                    }
                    else if (Block.Code.ToString().Contains("south"))
                    {
                        chkpos.X -= widthc;
                    }
                    else if (Block.Code.ToString().Contains("east"))
                    {
                        chkpos.Z += widthc;
                    }
                    else if (Block.Code.ToString().Contains("west"))
                    {
                        chkpos.Z -= widthc;
                    }
                    else
                    {
                        BreakMe();
                    }
                    AssetLocation al = new AssetLocation("machines:"+blocklist[indexc]);
                    if (al == null)
                    {
                        BreakMe();
                    }
                    Block newblock = Api.World.BlockAccessor.GetBlock(al);
                    if (newblock == null)
                    {
                        BreakMe();
                    }
                    Api.World.BlockAccessor.SetBlock(newblock.BlockId, chkpos);
                    ISlaveBlock isb = Api.World.BlockAccessor.GetBlockEntity(chkpos) as ISlaveBlock;
                    if (isb != null)
                    {
                        SlaveBlocks.Add(isb);
                        isb.Initialize(this);
                    }
                    indexc++;
                } 
            }
        }
    }
}
