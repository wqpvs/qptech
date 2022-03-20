using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.API.Server;

namespace chisel.src
{
    /// <summary>
    /// First copy a block - hold left click to copy, maybe use some cloth and dye?
    /// Work a new block into that shape, hold right click to apply it
    /// </summary>
    
    class ItemPantograph:Item
    {
        List<uint> copiedblockvoxels;
        List<int> copiedblockmaterials;
        string copiedname;
        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            if (blockSel == null) { return; }
            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            {
                byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
                return;
            }
            BlockEntityMicroBlock bmb = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityMicroBlock;
            if (bmb == null|| bmb.VoxelCuboids == null || bmb.VoxelCuboids.Count == 0) { base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling); return; }

            handling = EnumHandHandling.PreventDefaultAction;
            copiedname = bmb.BlockName;
            copiedblockvoxels = new List<uint>();
            copiedblockmaterials = new List<int>();
            foreach (uint u in bmb.VoxelCuboids)
            {
                copiedblockvoxels.Add(u);
            }
            foreach (int m in bmb.MaterialIds)
            {
                copiedblockmaterials.Add(m);
            }
        }
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (blockSel == null) { return; }
            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            {
                byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
                return;
            }
            BlockEntityMicroBlock bmb = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityMicroBlock;
            if (copiedblockvoxels==null|| bmb == null || bmb.VoxelCuboids == null || bmb.VoxelCuboids.Count == 0) { base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling); return; }
                        
            if (bmb.MaterialIds.Length < copiedblockmaterials.Count) //if we have a material mismatch just use material 0 for everything
            {

                CuboidWithMaterial cwm = new CuboidWithMaterial();
                BlockEntityMicroBlock.FromUint(bmb.VoxelCuboids[0], cwm);
                byte useindex = cwm.Material;
                   
                bmb.VoxelCuboids = CuboidStripMaterials(copiedblockvoxels,0);
            }
            else { bmb.VoxelCuboids = new List<uint>(copiedblockvoxels); }
            
            bmb.BlockName = "Copy of " + copiedname;
            bmb.MarkDirty(true);
            handling = EnumHandHandling.PreventDefaultAction;
        }

        /// <summary>
        /// Returns a list of compressed cuboids all using the supplied material index
        /// </summary>
        /// <param name="originalcuboids">List of CuboidsWithMaterials packed into uint</param>
        /// <param name="newmat">Material to use</param>
        /// <returns></returns>
        public static List<uint> CuboidStripMaterials(List<uint> originalcuboids,byte newmat)
        {
            List<uint> newcuboid = new List<uint>();
            foreach (uint og in originalcuboids)
            {
                CuboidWithMaterial cwm = new CuboidWithMaterial();
                BlockEntityMicroBlock.FromUint(og, cwm);
                
                newcuboid.Add(BlockEntityMicroBlock.ToUint(cwm.MinX, cwm.MinY, cwm.MinZ, cwm.MaxX, cwm.MaxY, cwm.MaxZ, newmat));
            }
            return newcuboid;
        }
    }
}
