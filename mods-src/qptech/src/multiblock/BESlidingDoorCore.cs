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
        bool isChangingState = false;
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

            ClearBlocks();
            base.OnBlockBroken();
        }
        void ClearBlocks()
        {
            foreach (ISlaveBlock isb in SlaveBlocks)
            {
                BlockEntity sbe = isb as BlockEntity;
                if (sbe == null) { continue; }
                BlockPos usepos = sbe.Pos.Copy();
                Api.World.BlockAccessor.RemoveBlockEntity(usepos);
                Api.World.BlockAccessor.SetBlock(0, usepos);

            }
            SlaveBlocks = new List<ISlaveBlock>();
        }
        public void BreakMe()
        {
            ClearBlocks();
            Api.World.BlockAccessor.BreakBlock(Pos,null);
        }
        /// <summary>
        /// verifies area is clear and we can make our door
        /// </summary>
        /// <returns>true or false</returns>
        public bool VerifyClear()
        {
            string[] blocklist = Block.Attributes["blocks"].AsArray<string>();
            //if no valid block list will just destroy itself
            if (blocklist == null || blocklist.Length < blocksHigh * blocksWide)
            {

                return false;
            }
            int indexc = 0;
            for (int heightc = 0; heightc < blocksHigh; heightc++)
            {
                for (int widthc = 0; widthc < blocksWide; widthc++)
                {
                    if (heightc == 0 && widthc == 0) { indexc++; continue; } //this should be our position - will add offsets later but skip
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
                        return false;
                    }
                    Block existingblock = Api.World.BlockAccessor.GetBlockOrNull(chkpos.X, chkpos.Y, chkpos.Z);
                    
                    
                    AssetLocation al = new AssetLocation("machines:" + blocklist[indexc]);
                    if (al == null)
                    {
                        return false;
                    }
                    Block newblock = Api.World.BlockAccessor.GetBlock(al);
                    if (newblock == null)
                    {
                        return false;
                    }
                    if (existingblock.Code == newblock.Code)
                    {
                        ISlaveBlock isb = Api.World.BlockAccessor.GetBlockEntity(chkpos) as ISlaveBlock;
                        if (isb == null) { Api.World.BlockAccessor.SetBlock(0, chkpos); }
                        else { isb.Initialize(this); }
                        return true;
                    }
                    if (!existingblock.Code.ToString().Contains("air")) { return false; }
                }
            }
            return true;
        }
        public void OnMemberRemoved()
        {
            if (!isChangingState) { BreakMe(); }
        }
        int packetChangeState = 99990001;

        public void Interact(IPlayer player)
        {
            //if (!Api is ICoreClientAPI)
            //(Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, (int)packetChangeState, null);
            if (!isChangingState) { ChangeState(); }
        }

        public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
        {
            if (packetid == packetChangeState&&!isChangingState) { ChangeState(); }
            base.OnReceivedClientPacket(fromPlayer, packetid, data);
        }

        public void ChangeState()
        {
            if (isChangingState) { return; }
            VerifyClear();
            isChangingState = true;
            ClearBlocks();
            string replacement = "machines:" + Block.Attributes["replacement"].AsString();
            AssetLocation al = new AssetLocation(replacement);
            Block replacementBlock = Api.World.BlockAccessor.GetBlock(al);
            if (replacementBlock == null) { BreakMe(); }
            Api.World.BlockAccessor.SetBlock(replacementBlock.Id, Pos);
        }
        public void SetupBlocks()
        {
            if (Api is ICoreClientAPI) { return; }
            
            if (!VerifyClear()) { return; }
            int indexc = 0;
            string[] blocklist = Block.Attributes["blocks"].AsArray<string>();
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
                    AssetLocation al = new AssetLocation("machines:"+blocklist[indexc]);
                    Block newblock = Api.World.BlockAccessor.GetBlock(al);
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
