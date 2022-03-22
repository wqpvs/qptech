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
        int copiedvolume;
        List<uint> undovoxels;
        BlockPos undoposition;
        string copiedname;
        SkillItem[] toolModes;
        WorldInteraction[] interactions;
        ICoreClientAPI capi;
        public enum enModes {FULLPASTE,ADDPASTE,UNDO}
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            if (api is ICoreClientAPI) { capi = api as ICoreClientAPI; }
            toolModes = ObjectCacheUtil.GetOrCreate(api, "pantographToolModes", () =>
            {
                SkillItem[] modes;
                
                modes = new SkillItem[3];
                modes[(int)enModes.FULLPASTE] = new SkillItem() { Code = new AssetLocation(enModes.FULLPASTE.ToString()), Name = Lang.Get("Replace with Copied Shape Mode") };
                modes[(int)enModes.ADDPASTE] = new SkillItem() { Code = new AssetLocation(enModes.ADDPASTE.ToString()), Name = Lang.Get("Add Copied Shape Mode") };
                modes[(int)enModes.UNDO] = new SkillItem() { Code = new AssetLocation(enModes.UNDO.ToString()), Name = Lang.Get("Undo Last Block Change") };

                if (capi != null)
                {
                    modes[(int)enModes.FULLPASTE].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/copy.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                    modes[(int)enModes.FULLPASTE].TexturePremultipliedAlpha = false;
                    modes[(int)enModes.ADDPASTE].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/add.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                    modes[(int)enModes.ADDPASTE].TexturePremultipliedAlpha = false;
                    modes[(int)enModes.UNDO].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/undo.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                    modes[(int)enModes.UNDO].TexturePremultipliedAlpha = false;
                }


                return modes;
            });
            interactions = ObjectCacheUtil.GetOrCreate(api, "PantographInteractions", () =>
            {
               
                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "Paste Shape",
                        MouseButton = EnumMouseButton.Right,
                        
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "Copy Shape",
                        MouseButton = EnumMouseButton.Left,

                    }
                };
            });
        }

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
            undovoxels = null;
            handling = EnumHandHandling.PreventDefaultAction;
            copiedname = bmb.BlockName;
            copiedblockvoxels = new List<uint>();
            copiedblockmaterials = new List<int>();
            copiedvolume = (int)(bmb.VolumeRel * 16f * 16f * 16f);
            foreach (uint u in bmb.VoxelCuboids)
            {
                copiedblockvoxels.Add(u);
                
            }
            foreach (int m in bmb.MaterialIds)
            {
                copiedblockmaterials.Add(m);
            }
            api.World.PlaySoundAt(new AssetLocation("sounds/player/chalkdraw1"), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer, true, 12, 1);
            //TODO add a special sound?
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
            
            undovoxels = new List<uint>(bmb.VoxelCuboids);
            undoposition = blockSel.Position;
            int changedvoxels = 0;
            int originalvolume = (int)(bmb.VolumeRel*16f*16f*16f);
            //normal copy
            if (slot.Itemstack.Attributes.GetInt("toolMode", (int)enModes.FULLPASTE)==(int)enModes.FULLPASTE)
            {
                if (bmb.MaterialIds.Length < copiedblockmaterials.Count) //if we have a material mismatch just use material 0 for everything
                {

                    CuboidWithMaterial cwm = new CuboidWithMaterial();
                    BlockEntityMicroBlock.FromUint(bmb.VoxelCuboids[0], cwm);
                    byte useindex = cwm.Material;

                    bmb.VoxelCuboids = CuboidStripMaterials(copiedblockvoxels, 0);
                }
                else { bmb.VoxelCuboids = new List<uint>(copiedblockvoxels); }
                changedvoxels = originalvolume + copiedvolume;
            }
            //boolean merge
            else
            {
                byte maxindex = (byte)bmb.MaterialIds.Length;
                //cycle thru source cuboids - turn into individual voxels, add to destination object
               foreach (uint su in copiedblockvoxels)
                {
                    CuboidWithMaterial cwm = new CuboidWithMaterial();
                    BlockEntityMicroBlock.FromUint(su, cwm);
                    if (cwm.Material >= maxindex) { cwm.Material = 0; } //make sure source materials aren't out of range
                    bool setthisvoxel = true;
                    //cycle through each voxel of the source cuboid and see if it's safe to write to the destination block
                    

                    for (int xc = cwm.X1; xc < cwm.X2; xc++)
                    {
                        for (int yc = cwm.Y1; yc < cwm.Y2; yc++)
                        {
                            for (int zc = cwm.Z1; zc < cwm.Z2; zc++)
                            {
                                //this is a lot of recursion but should be ok - could do a check of intersecting cuboids first but probably doesn't save much
                                setthisvoxel = true;
                                foreach (uint du in bmb.VoxelCuboids)
                                {
                                    CuboidWithMaterial dcwm = new CuboidWithMaterial();
                                    BlockEntityMicroBlock.FromUint(du, dcwm);
                                    if (dcwm.Contains(xc, yc, zc))
                                    {
                                        setthisvoxel = false;
                                        break;
                                    }
                                }
                                if (setthisvoxel)
                                {
                                    bmb.SetVoxel(new Vec3i(xc, yc, zc), true, null, cwm.Material, 1);
                                    changedvoxels++;
                                }
                            }
                            
                        }
                        
                    }
                    
                }
                bmb.MarkDirty(true);
                
            }
            bmb.BlockName = "Copy of " + copiedname.Replace("Copy of ","");
            bmb.MarkDirty(true);
            if (api is ICoreServerAPI && byPlayer?.WorldData.CurrentGameMode != EnumGameMode.Creative)
            {

                this.DamageItem(api.World, byEntity, byPlayer.InventoryManager.ActiveHotbarSlot, CalcDamage(changedvoxels));

            }
            handling = EnumHandHandling.PreventDefaultAction;
        }
        protected virtual int CalcDamage(int numvoxels)
        {

            int basedamage = (int)(ChiselToolLoader.serverconfig.pantographBaseDurabilityMultiplier * (float)numvoxels);
            if (basedamage < 0) { basedamage = 0; }
            return basedamage;
        }
        public virtual void Undo()
        {
            if (undovoxels != null)
            {
                BlockEntityMicroBlock bmb = api.World.BlockAccessor.GetBlockEntity(undoposition) as BlockEntityMicroBlock;
                if (bmb == null) { undovoxels = null; }
                bmb.VoxelCuboids = new List<uint>(undovoxels);
                bmb.MarkDirty(true);
                undovoxels = null;
            }
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
        public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
        {
            return toolModes;
        }

        public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
        {
            return Math.Min(toolModes.Length - 1, slot.Itemstack.Attributes.GetInt("toolMode"));
        }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel, int toolMode)
        {
            slot.Itemstack.Attributes.SetInt("toolMode", toolMode);
            if (toolMode == (int)enModes.UNDO)
            {
                Undo();
                SetToolMode(slot, byPlayer, blockSel, slot.Itemstack.Attributes.GetInt("lastToolMode",0));
            }
            else
            {
                slot.Itemstack.Attributes.SetInt("lastToolMode", toolMode);
            }
        }
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            dsc.AppendLine("Left Click to Copy, Right Click to Paste");
            if (copiedblockvoxels != null) { dsc.AppendLine("Copying " + copiedname); }
            if (undovoxels != null) { dsc.AppendLine("Undo is currently available"); }
           
        }
        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return interactions.Append(base.GetHeldInteractionHelp(inSlot));
        }
    }
}
