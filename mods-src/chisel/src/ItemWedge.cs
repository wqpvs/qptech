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
    class ItemWedge:Item
    {
        //These will track the last thing the planer clicked on
        protected BlockPos lastpos;
        protected BlockFacing lastfacing = BlockFacing.DOWN;
        List<uint> undovoxels;
        BlockPos undoposition;
        SkillItem[] toolModes;
        WorldInteraction[] interactions;
        ICoreClientAPI capi;
        public enum enModes { MOVE,FLIP,ROTATE,UNDO }
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            if (api is ICoreClientAPI) { capi = api as ICoreClientAPI; }
            toolModes = ObjectCacheUtil.GetOrCreate(api, "wedgeToolModes", () =>
            {
                SkillItem[] modes;

                modes = new SkillItem[4];
                modes[(int)enModes.MOVE] = new SkillItem() { Code = new AssetLocation(enModes.MOVE.ToString()), Name = Lang.Get("Move Block") };
                modes[(int)enModes.FLIP] = new SkillItem() { Code = new AssetLocation(enModes.FLIP.ToString()), Name = Lang.Get("Mirror Block") };
                modes[(int)enModes.ROTATE] = new SkillItem() { Code = new AssetLocation(enModes.FLIP.ToString()), Name = Lang.Get("Rotate Block") };
                modes[(int)enModes.UNDO] = new SkillItem() { Code = new AssetLocation(enModes.UNDO.ToString()), Name = Lang.Get("Undo Last Block Change") };

                if (capi != null)
                {
                    modes[(int)enModes.MOVE].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/push.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                    modes[(int)enModes.MOVE].TexturePremultipliedAlpha = false;
                    modes[(int)enModes.FLIP].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/flip.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                    modes[(int)enModes.FLIP].TexturePremultipliedAlpha = false;
                    modes[(int)enModes.ROTATE].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/rotation.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                    modes[(int)enModes.ROTATE].TexturePremultipliedAlpha = false;
                    modes[(int)enModes.UNDO].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/undo.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                    modes[(int)enModes.UNDO].TexturePremultipliedAlpha = false;
                }


                return modes;
            });
            interactions = ObjectCacheUtil.GetOrCreate(api, "WedgeInteractions", () =>
            {

                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "Perform Action",
                        MouseButton = EnumMouseButton.Right,

                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "Open Menu",
                        HotKeyCode="f",

                    }
                };
            });
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
                SetToolMode(slot, byPlayer, blockSel, slot.Itemstack.Attributes.GetInt("lastToolMode", 0));
            }
            else
            {
                slot.Itemstack.Attributes.SetInt("lastToolMode", toolMode);
            }
        }
        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {

            if(blockSel == null) { return; }
            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            {
                byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
                return;
            }
            int cutvoxels = 0;
            Backup(blockSel.Position);
            if (slot.Itemstack.Attributes.GetInt("lastToolMode", (int)enModes.MOVE) == (int)enModes.MOVE)
            {
                cutvoxels = MoveChiseledBlock(blockSel);
            }
            else if (slot.Itemstack.Attributes.GetInt("lastToolMode", (int)enModes.MOVE) == (int)enModes.FLIP)
            {
                cutvoxels = MirrorBlock(blockSel);
            }
            else if (slot.Itemstack.Attributes.GetInt("lastToolMode", (int)enModes.MOVE) == (int)enModes.ROTATE)
            {
                cutvoxels = RotateBlock(blockSel);
            }
            if (cutvoxels > 0)
            {
                api.World.PlaySoundAt(new AssetLocation("chiseltools:sounds/stone_move"), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer, true, 12, 0.25f);
            }
            if (api is ICoreServerAPI)
            {
                if (byPlayer?.WorldData.CurrentGameMode == EnumGameMode.Creative)
                {
                    handling = EnumHandHandling.PreventDefaultAction;
                }
                else
                {
                    this.DamageItem(api.World, byEntity, byPlayer.InventoryManager.ActiveHotbarSlot, CalcDamage(cutvoxels));
                }
            }

            handling = EnumHandHandling.PreventDefaultAction;
        }

        protected virtual int CalcDamage(int numvoxels)
        {
            int basedamage = ChiselToolLoader.serverconfig.handPlanerBaseDurabilityUse * numvoxels;
            basedamage = (int)(ChiselToolLoader.serverconfig.handPlanerBaseDurabilityMultiplier * (float)numvoxels);
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

        public virtual void Backup(BlockPos pos)
        {
            undovoxels = null;
            undoposition = pos;
            BlockEntityMicroBlock bmb = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityMicroBlock;
            if (bmb == null) { return; }
            undovoxels = new List<uint>(bmb.VoxelCuboids);
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
            if (bmb == null)
            {
                lastpos = null;

                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                return;
            }
            int cutvoxels = 0;
            Backup(blockSel.Position);
            if (slot.Itemstack.Attributes.GetInt("lastToolMode", (int)enModes.MOVE) == (int)enModes.MOVE)
            {
                cutvoxels = MoveChiseledBlock(blockSel);
            }
            else if (slot.Itemstack.Attributes.GetInt("lastToolMode", (int)enModes.MOVE) == (int)enModes.FLIP)
            {
                cutvoxels = MirrorBlock(blockSel);
            }
            else if (slot.Itemstack.Attributes.GetInt("lastToolMode", (int)enModes.MOVE) == (int)enModes.ROTATE)
            {
                cutvoxels = RotateBlock(blockSel);
            }
            if (cutvoxels > 0)
            {
                api.World.PlaySoundAt(new AssetLocation("chiseltools:sounds/stone_move"), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer, true, 12, 0.25f);
            }
            if (api is ICoreServerAPI)
            {
                if (byPlayer?.WorldData.CurrentGameMode == EnumGameMode.Creative)
                {
                    handling = EnumHandHandling.PreventDefaultAction;
                }
                else
                {
                    this.DamageItem(api.World, byEntity, byPlayer.InventoryManager.ActiveHotbarSlot, CalcDamage(cutvoxels));
                }
            }

            handling = EnumHandHandling.PreventDefaultAction;
        }

        /// <summary>
        /// Add or remove faces only matching a certain material
        /// Returns voxels cut
        /// </summary>
        /// <param name="mode">Mode selector for the function</param>

        
        public virtual int MoveChiseledBlock(BlockSelection blockSel)
        {
            BlockEntityMicroBlock bmb = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityMicroBlock;
            if (bmb == null) { lastpos = null; return 0; }
            

            Vec3i writeoffset = new Vec3i(0, 0, 0);
            
            //adjust the plane based on facing
            if (blockSel.Face == BlockFacing.SOUTH) { writeoffset.Z = -1; }
            else if (blockSel.Face==BlockFacing.NORTH) { writeoffset.Z = 1; }
            else if (blockSel.Face == BlockFacing.EAST) { writeoffset.X = -1; }
            else if (blockSel.Face==BlockFacing.WEST){ writeoffset.X = 1; }
            else if (blockSel.Face == BlockFacing.UP) { writeoffset.Y = -1; }
            else if (blockSel.Face==BlockFacing.DOWN){ writeoffset.Y = 1; }

            List<uint> cuboids = new List<uint>();
            //Loop thru the cuboids and find the material we are looking at
            foreach (uint voxint in bmb.VoxelCuboids)
            {
                CuboidWithMaterial cwm = new CuboidWithMaterial();
                //this static method converts the uint into a CubioidWithMaterial
                BlockEntityMicroBlock.FromUint(voxint, cwm);
                //Shift coordinates of every cuboid - possible issue - voxels stacking up at outside of object
                cwm.X1 += writeoffset.X;cwm.X1 = GameMath.Clamp(cwm.X1, 0, 16);
                cwm.X2 += writeoffset.X;cwm.X2 = GameMath.Clamp(cwm.X2, 0, 16);
                if (cwm.X2 <= cwm.X1) { continue; }
                cwm.Y1 += writeoffset.Y; cwm.Y1 = GameMath.Clamp(cwm.Y1, 0, 16);
                cwm.Y2 += writeoffset.Y; cwm.Y2 = GameMath.Clamp(cwm.Y2, 0, 16);
                if (cwm.Y2 <= cwm.Y1) { continue; }
                cwm.Z1 += writeoffset.Z; cwm.Z1 = GameMath.Clamp(cwm.Z1, 0, 16);
                cwm.Z2 += writeoffset.Z; cwm.Z2 = GameMath.Clamp(cwm.Z2, 0, 16);
                if (cwm.Z2 <= cwm.Z1) { continue; }
                uint voxelint = BlockEntityMicroBlock.ToUint(cwm.X1, cwm.Y1, cwm.Z1, cwm.X2, cwm.Y2, cwm.Z2, cwm.Material);
                cuboids.Add(voxelint);
            }
            if (cuboids == null || cuboids.Count == 0) { return 0; }
            bmb.VoxelCuboids = cuboids;
            bmb.MarkDirty(true);
            return 16*16; //todo calculate actual voxels based on the bounds of the cuboids
        }
        public virtual int MirrorBlock(BlockSelection blockSel)
        {
            BlockEntityMicroBlock bmb = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityMicroBlock;
            if (bmb == null) { lastpos = null; return 0; }
            if (bmb.VoxelCuboids == null || bmb.VoxelCuboids.Count == 0) { return 0; }
            List<uint> cuboids = new List<uint>();
            
            foreach (uint voxint in bmb.VoxelCuboids)
            {
                CuboidWithMaterial cwm = new CuboidWithMaterial();
                //this static method converts the uint into a CubioidWithMaterial
                BlockEntityMicroBlock.FromUint(voxint, cwm);
                //x&z should stay the same
                if (blockSel.Face == BlockFacing.UP || blockSel.Face == BlockFacing.DOWN)
                {
                    int y1 = 16 - cwm.Y2;
                    int y2 = 16 - cwm.Y1;
                    cwm.Y1 = y1;
                    cwm.Y2 = y2;
                }
                else if (blockSel.Face== BlockFacing.EAST || blockSel.Face == BlockFacing.WEST)
                {
                    int z1 = 16 - cwm.Z2;
                    int z2 = 16 - cwm.Z1;
                    cwm.Z1 = z1;
                    cwm.Z2 = z2;
                }
                else if (blockSel.Face == BlockFacing.NORTH || blockSel.Face == BlockFacing.SOUTH)
                {
                    int x1 = 16 - cwm.X2;
                    int x2 = 16 - cwm.X1;
                    cwm.X1 = x1;
                    cwm.X2 = x2;
                }
                cuboids.Add(BlockEntityMicroBlock.ToUint(cwm.MinX, cwm.MinY, cwm.MinZ, cwm.MaxX, cwm.MaxY, cwm.MaxZ, cwm.Material));

            }

            
            if (cuboids == null || cuboids.Count == 0) { return 0; }
            bmb.VoxelCuboids = cuboids;
            bmb.MarkDirty(true);
            return 16 * 16; //todo calculate actual voxels based on the bounds of the cuboids
        }
        public virtual int RotateBlock(BlockSelection blockSel)
        {
            BlockEntityMicroBlock bmb = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityMicroBlock;
            if (bmb == null) { lastpos = null; return 0; }
            if (bmb.VoxelCuboids == null || bmb.VoxelCuboids.Count == 0) { return 0; }
            List<uint> cuboids = new List<uint>();
            double angle = 90;
            foreach (uint voxint in bmb.VoxelCuboids)
            {
                CuboidWithMaterial cwm = new CuboidWithMaterial();
                //this static method converts the uint into a CubioidWithMaterial
                BlockEntityMicroBlock.FromUint(voxint, cwm);
                Cuboidi torotate = cwm as Cuboidi;
                if (blockSel.Face == BlockFacing.EAST || blockSel.Face == BlockFacing.WEST)
                {
                    torotate = torotate.RotatedCopy(new Vec3d(angle, 0, 0), new Vec3d(8, 8, 8));
                }
                else if (blockSel.Face == BlockFacing.NORTH || blockSel.Face == BlockFacing.SOUTH)
                {
                    torotate = torotate.RotatedCopy(new Vec3d(0, 0, angle), new Vec3d(8, 8, 8));
                }
                else
                {
                    torotate = torotate.RotatedCopy(new Vec3d(0, angle, 0), new Vec3d(8, 8, 8));
                }
                //x&z should stay the same

                cuboids.Add(BlockEntityMicroBlock.ToUint(torotate.MinX,torotate.MinY,torotate.MinZ,torotate.MaxX,torotate.MaxY,torotate.MaxZ,  cwm.Material));

            }

            if (cuboids == null || cuboids.Count == 0) { return 0; }
            bmb.VoxelCuboids = cuboids;
            bmb.MarkDirty(true);
            return 16 * 16; //todo calculate actual voxels based on the bounds of the cuboids
        }
    }
}
