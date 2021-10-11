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
    class BEReportsClicks:BlockEntity,ISlaveBlock
    {
        BlockPos master;
        bool initialized = false;
        public BlockPos Master => master;
        public bool Initialized => initialized&&master!=null;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
        }
        public void Initialize(BlockPos masterpos)
        {
            master=masterpos;
            initialized = true;
            this.MarkDirty(true);
        }
        public override void OnBlockRemoved()
        {
            if (initialized&&master!=null)
            {
                IMasterBlock mb = Api.World.BlockAccessor.GetBlockEntity(master) as IMasterBlock;
                if (mb != null)
                {
                    mb.OnMemberRemoved();
                }
            }
            base.OnBlockRemoved();
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            if (master == null) { initialized = false; }
            tree.SetBool("Initialized", initialized);
            if (initialized && master != null)
            {
                tree.SetInt("masterx", master.X);
                tree.SetInt("mastery", master.Y);
                tree.SetInt("masterz", master.Z);
            }
            base.ToTreeAttributes(tree);
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            initialized = tree.GetBool("Initialized", false);
            if (initialized && Pos!=null)
            {
                BlockPos master = Pos.Copy();
                master.X = tree.GetInt("masterx", master.X);
                master.Y = tree.GetInt("masterx", master.Y);
                master.Z = tree.GetInt("masterz", master.Z);
            }
            else
            {
                initialized = false;
            }
            base.FromTreeAttributes(tree, worldAccessForResolve);
        }

        public void Interact(IPlayer player)
        {
            if (!Initialized) { return; }
            IMasterBlock mb = Api.World.BlockAccessor.GetBlockEntity(master) as IMasterBlock;
            if (mb != null)
            {
                mb.Interact(player);
            }
        }
    }
}
