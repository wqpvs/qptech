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
    /// The HandPlaner will add or remove 1 voxel deep planes to chiseled objects (adding or shaving off a full sheet of voxels)
    /// </summary>
    class ItemHandPlaner:Item
    {
        
        //These will track the last thing the planer clicked on
        protected BlockPos lastpos;
        protected BlockFacing lastfacing=BlockFacing.DOWN;
        
        SkillItem[] toolModes;
        WorldInteraction[] interactions;
        ICoreClientAPI capi;
        public enum enModes { PLANE, MATERIAL, UNDO }
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            if (api is ICoreClientAPI) { capi = api as ICoreClientAPI; }
            toolModes = ObjectCacheUtil.GetOrCreate(api, "PlanerToolModes", () =>
            {
                SkillItem[] modes;

                modes = new SkillItem[3];
                modes[(int)enModes.PLANE] = new SkillItem() { Code = new AssetLocation(enModes.PLANE.ToString()), Name = Lang.Get("Add/Remove any material") };
                modes[(int)enModes.MATERIAL] = new SkillItem() { Code = new AssetLocation(enModes.MATERIAL.ToString()), Name = Lang.Get("Add/Remove matching material") };
                modes[(int)enModes.UNDO] = new SkillItem() { Code = new AssetLocation(enModes.UNDO.ToString()), Name = Lang.Get("Undo Last Block Change") };

                if (capi != null)
                {
                    modes[(int)enModes.PLANE].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/plane.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                    modes[(int)enModes.PLANE].TexturePremultipliedAlpha = false;
                    modes[(int)enModes.MATERIAL].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/material.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                    modes[(int)enModes.MATERIAL].TexturePremultipliedAlpha = false;
                    modes[(int)enModes.UNDO].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/undo.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                    modes[(int)enModes.UNDO].TexturePremultipliedAlpha = false;
                }


                return modes;
            });
            interactions = ObjectCacheUtil.GetOrCreate(api, "PlanerInteractions", () =>
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
                Undo(slot);
                SetToolMode(slot, byPlayer, blockSel, slot.Itemstack.Attributes.GetInt("lastToolMode", 0));
            }
            else
            {
                slot.Itemstack.Attributes.SetInt("lastToolMode", toolMode);
            }
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
            if (api.ModLoader.GetModSystem<ModSystemBlockReinforcement>()?.IsReinforced(blockSel.Position) == true)
            {
                byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
                return;
            }
            BlockEntityMicroBlock bmb = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityMicroBlock;
            if (bmb == null)
            {
                TryChangeBlockToChisel(blockSel, byEntity, byPlayer);
                handling = EnumHandHandling.PreventDefaultAction;
                return;
            }
            int cutvoxels = 0;
            Backup(slot,blockSel.Position);
            if (slot.Itemstack.Attributes.GetInt("lastToolMode",(int) enModes.MATERIAL)==(int)enModes.MATERIAL)
            {
                cutvoxels=ModifyChiseledBlock(blockSel, "sneakleft");
            }
            else
            {
                cutvoxels=ModifyChiseledBlock(blockSel,"left");
            }
            if (cutvoxels > 0)
            {
                api.World.PlaySoundAt(new AssetLocation("sounds/stone_move") , blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer, true, 6, 0.25f);
                
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
        public bool TryChangeBlockToChisel(BlockSelection blockSel, Entity byEntity, IPlayer byPlayer)
        {
            Block bl = api.World.BlockAccessor.GetBlock(blockSel.Position);
            string blockName = bl.GetPlacedBlockName(byEntity.World, blockSel.Position);
            Block chiseledblock = byEntity.World.GetBlock(new AssetLocation("chiseledblock"));
            if (!IsChiselingAllowedFor(bl, byPlayer)) { return false; }
            byEntity.World.BlockAccessor.SetBlock(chiseledblock.BlockId, blockSel.Position);
            BlockEntityChisel be = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityChisel;
            if (be == null) return false;

            be.WasPlaced(bl, blockName);
            return true;
        }

        public bool IsChiselingAllowedFor(Block block, IPlayer player)
        {
            if (block is BlockChisel) return true;

            // First priority: microblockChiseling disabled
            ITreeAttribute worldConfig = api.World.Config;
            string mode = worldConfig.GetString("microblockChiseling");
            if (mode == "off") return false;


            // Second priority: canChisel flag
            bool canChiselSet = block.Attributes?["canChisel"].Exists == true;
            bool canChisel = block.Attributes?["canChisel"].AsBool(false) == true;

            if (canChisel) return true;
            if (canChiselSet && !canChisel) return false;


            // Third prio: Never non cubic blocks
            if (block.DrawType != EnumDrawType.Cube) return false;

            // Fourth prio: Never tinted blocks (because then the chiseled block would have the wrong colors)
            if (block.SeasonColorMap != null || block.ClimateColorMap != null) return false;

            // Otherwise if in creative mode, sure go ahead
            if (player?.WorldData.CurrentGameMode == EnumGameMode.Creative) return true;


            // Lastly go by the config value
            if (mode == "stonewood")
            {
                // Saratys definitely required Exception to the rule #312
                if (block.Code.Path.Contains("mudbrick")) return true;

                return block.BlockMaterial == EnumBlockMaterial.Wood || block.BlockMaterial == EnumBlockMaterial.Stone || block.BlockMaterial == EnumBlockMaterial.Ore || block.BlockMaterial == EnumBlockMaterial.Ceramic;
            }

            return true;
        }
        protected virtual int CalcDamage(int numvoxels)
        {
            int basedamage = ChiselToolLoader.serverconfig.handPlanerBaseDurabilityUse * numvoxels;
            basedamage=(int)(ChiselToolLoader.serverconfig.handPlanerBaseDurabilityMultiplier*(float)numvoxels);
            if (basedamage < 0) { basedamage = 0; }
            return basedamage;
        }
        public virtual void Undo(ItemSlot slot)
        {
            if (api is ICoreClientAPI) { return; }
            List<uint> undovoxels = null;
            BlockPos undoposition = null;
            try
            {
                byte[] voxdat = slot.Itemstack.Attributes.GetBytes("undovoxels", null);
                if (voxdat == null) { return; }
                undovoxels = SerializerUtil.Deserialize<List<uint>>(voxdat);
                if (undovoxels == null) { return; }
                undoposition = slot.Itemstack.Attributes.GetBlockPos("undoposition", null);
                if (undoposition == null) { return; }
                //slot.Itemstack.Attributes.GetBytes("undovoxels", SerializerUtil.Deserialize<List<uint>>(undovoxels));
                //slot.Itemstack.Attributes.SetBlockPos("undoposition", blockSel.Position);
            }
            catch
            {
                return;
            }

            if (undovoxels != null && undoposition != null)
            {
                BlockEntityMicroBlock bmb = api.World.BlockAccessor.GetBlockEntity(undoposition) as BlockEntityMicroBlock;
                if (bmb == null) { return; }
                bmb.VoxelCuboids = new List<uint>(undovoxels);
                bmb.MarkDirty(true);
                undovoxels = null;
            }
        }
        public override void OnUnloaded(ICoreAPI api)
        {
            for (int i = 0; toolModes != null && i < toolModes.Length; i++)
            {
                toolModes[i]?.Dispose();
            }


        }
        public virtual void Backup(ItemSlot slot, BlockPos pos)
        {
            BlockEntityMicroBlock bmb = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityMicroBlock;
            if (bmb == null) { return; }

            List<uint> undovoxels = new List<uint>(bmb.VoxelCuboids);

            if (api is ICoreServerAPI)
            {
                slot.Itemstack.Attributes.SetBytes("undovoxels", SerializerUtil.Serialize<List<uint>>(undovoxels));
                slot.Itemstack.Attributes.SetBlockPos("undoposition", pos);
                slot.MarkDirty();
            }

        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (blockSel == null) { return; }
            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            if (api.ModLoader.GetModSystem<ModSystemBlockReinforcement>()?.IsReinforced(blockSel.Position) == true)
            {
                byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
                return;
            }
            if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            {
                byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
                return;
            }
            BlockEntityMicroBlock bmb = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityMicroBlock;
            if (bmb == null) {
                lastpos = null;
                TryChangeBlockToChisel(blockSel, byEntity, byPlayer);
                handling = EnumHandHandling.PreventDefaultAction;
                return;
            }
            
            int cutvoxels = 0;
            Backup(slot,blockSel.Position);
            if (slot.Itemstack.Attributes.GetInt("lastToolMode", (int)enModes.MATERIAL) == (int)enModes.MATERIAL)
            {
                cutvoxels=ModifyChiseledBlock(blockSel, "sneakright");
            }
            else
            {
                cutvoxels = ModifyChiseledBlock(blockSel,"right");
            }
            if (cutvoxels > 0)
            {
                api.World.PlaySoundAt(new AssetLocation("sounds/stone_move"), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer, true, 12, 0.25f);
                handling = EnumHandHandling.PreventDefaultAction;
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
            

        }
        
        /// <summary>
        /// Add or remove faces only matching a certain material
        /// Returns voxels cut
        /// </summary>
        /// <param name="mode">Mode selector for the function</param>
        
        public virtual int ModifyChiseledBlock(BlockSelection blockSel,string mode)
        {
            bool addmode = false;
            bool onlymatching = false;
            if (mode == "sneakleft")
            {
                addmode = false;
                onlymatching = true;
            }
            else if (mode == "sneakright")
            {
                addmode = true;
                onlymatching = true;
            }
            else if (mode == "right")
            {
                addmode = true;
                onlymatching = false;
            }
            if (blockSel == null)
            {
                lastpos = null; return 0;
            }
            if (lastpos == null || lastpos != blockSel.Position || lastfacing != blockSel.Face)
            {
                lastpos = blockSel.Position;
                lastfacing = blockSel.Face;

            }
            BlockEntityMicroBlock bmb = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityMicroBlock;
            if (bmb == null) { lastpos = null; return 0; }
            byte useindex = 0;
            CuboidWithMaterial cwm = new CuboidWithMaterial();
            //convert the hit point into voxel coordinates
            Vec3i s = new Vec3i((int)(blockSel.HitPosition.X * 16f), (int)(blockSel.HitPosition.Y * 16f), (int)(blockSel.HitPosition.Z * 16f));
            if (blockSel.Face == BlockFacing.SOUTH) { s.Z--; }
            if (blockSel.Face == BlockFacing.UP) { s.Y--; }
            if (blockSel.Face == BlockFacing.EAST) { s.X--; }

            List<CuboidWithMaterial> cuboidsInPlane = new List<CuboidWithMaterial>() ;
            Cuboidi checkplane = new Cuboidi();
            Vec3i startco = new Vec3i(s.X,s.Y,s.Z); //start coordinates of our build plane
            Vec3i endco = new Vec3i(s.X, s.Y, s.Z); //end coordinates of our build plane
            Vec3i writeoffset = new Vec3i(0,0,0);

            
            //adjust the plane based on facing
            if (blockSel.Face == BlockFacing.NORTH||blockSel.Face==BlockFacing.SOUTH)
            {
                startco.X = 0;startco.Y = 0;
                endco.X = 16;endco.Y = 16;
                endco.Z += 1;
                if (addmode)
                {
                    if (blockSel.Face == BlockFacing.SOUTH) { writeoffset.Z = 1; }
                    else { writeoffset.Z = -1; }
                }
            }
            else if (blockSel.Face == BlockFacing.EAST || blockSel.Face == BlockFacing.WEST)
            {
                startco.Z = 0; startco.Y = 0;
                endco.Z = 16;endco.Y = 16;
                endco.X += 1;
                if (addmode)
                {
                    if (blockSel.Face == BlockFacing.EAST) { writeoffset.X = 1; }
                    else { writeoffset.X = -1; }
                }
            }
            else if (blockSel.Face == BlockFacing.DOWN || blockSel.Face == BlockFacing.UP)
            {
                startco.X = 0;startco.Z = 0;
                endco.X = 16;endco.Z = 16;
                endco.Y += 1;
                if (addmode)
                {
                    if (blockSel.Face == BlockFacing.UP) { writeoffset.Y = 1; }
                    else { writeoffset.Y = -1; }
                }
            }
            checkplane.Set(startco, endco);

            //Loop thru the cuboids and find the material we are looking at
            foreach (uint voxint in bmb.VoxelCuboids)
            {
                //this static method converts the uint into a CubioidWithMaterial
                BlockEntityMicroBlock.FromUint(voxint, cwm);
                //if we're looking at this voxel we want to grab this material to use
                if (cwm.Contains(s.X, s.Y, s.Z)) { useindex = cwm.Material;break;  }    
            }
            
            //now we go thru the cuboids again - we need to grab cuboids that intersect with our plane and that have the appropriate material
            foreach (uint voxint in bmb.VoxelCuboids)
            {
                
                CuboidWithMaterial checkcuboid = new CuboidWithMaterial();
                BlockEntityMicroBlock.FromUint(voxint, checkcuboid);//convert raw data into a cuboid we can read
                if (onlymatching&& checkcuboid.Material != useindex) { continue; }//reject cuboids that don't share our material
                if (checkcuboid.Intersects(checkplane)) { cuboidsInPlane.Add(checkcuboid); }//if it's in the check plane then add it
            }
            if (onlymatching&&cuboidsInPlane.Count == 0) { return 0; } //I don't think this is necessary, but maybe if there's a race condition?

            //loop thru 16 x 16 voxel plane (xc/yc will be swapped with other coordinates depending on the direction we are facing)
            //tell the microblock to add each voxel
            //adjust the start and end coordinates to work with the for loop:
            if (endco.X == startco.X) { endco.X++; }
            if (endco.Y == startco.Y) { endco.Y++; }
            if (endco.Z == startco.Z) { endco.Z++; }
            int cutvoxels = 0;
            //Loop thru all the dimensions of our plane (one dimension should be 0 size)
            for (int xc = startco.X; xc < endco.X; xc++)
            {
                for (int yc= startco.Y; yc < endco.Y; yc++)
                {
                    for (int zc = startco.Z; zc < endco.Z; zc++)
                    {
                        BlockPos v = new BlockPos(xc, yc, zc);
                        if (v.X + writeoffset.X > 15 || v.X + writeoffset.X < 0) { continue; }
                        if (v.Y + writeoffset.Y > 15 || v.Y + writeoffset.Y < 0) { continue; }
                        if (v.Z + writeoffset.Z > 15 || v.Z + writeoffset.Z < 0) { continue; }

                        if (onlymatching)
                        {
                            foreach (CuboidWithMaterial cube in cuboidsInPlane)
                            {
                                if (!cube.Contains(v)) { continue; }

                                bmb.SetVoxel(new Vec3i(v.X + writeoffset.X, v.Y + writeoffset.Y, v.Z + writeoffset.Z), addmode, null, useindex, 1);
                                cutvoxels++;
                                break;
                            }
                        }
                        else
                        {
                            if (addmode)
                            {
                                bmb.SetVoxel(new Vec3i(v.X + writeoffset.X, v.Y + writeoffset.Y, v.Z + writeoffset.Z), false, null, useindex, 1);
                            }
                            bmb.SetVoxel(new Vec3i(v.X + writeoffset.X, v.Y + writeoffset.Y, v.Z + writeoffset.Z), addmode, null, useindex, 1);
                            cutvoxels++;
                        }
                    }
                }
            }

            bmb.MarkDirty(true);
            return cutvoxels;
        }

    }
}
