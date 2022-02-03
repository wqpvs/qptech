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
    /// <summary>
    /// This is an invisible placeholder block for oversized blocks
    /// </summary>
    class BEDummyBlock:BlockEntity
    {
        IDummyParent parentblock;
        public IDummyParent ParentBlock => parentblock;
        bool informed = false;
        public string displaytext = "no parent";
        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            if (parentblock != null && !informed&&Api is ICoreServerAPI) { informed = true; parentblock.OnDummyBroken(); }
            base.OnBlockBroken(byPlayer);
        }
        public void ParentBroken()
        {
            informed = true;
            Api.World.BlockAccessor.BreakBlock(Pos,null);
        }
        public void SetParent(IDummyParent newparent)
        {
            parentblock = newparent;
            MarkDirty();
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if (parentblock == null) { dsc.Append(displaytext); }
            else { dsc.Append(parentblock.ToString()); }
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            if (parentblock != null) { tree.SetString("displaytext", parentblock.GetDisplayName()); }
            
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            displaytext = tree.GetString("displaytext");
        }
        
    }

    

    public interface IDummyParent
    {
        string GetDisplayName();
        void OnDummyBroken();

    }
}
