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
    
    class ItemPaintBrush:Item
    {
        //TODO - add tool wear in survival
        //  - add paintbrush graphic and sound effect
        //  - add check to not use paintbrush if materials match
        const int inkslotnumber = 0;
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            if (blockSel == null) { return; }
            
            //Do nothing if we don't have access to selected block
            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
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

            //do nothing if we don't have a valid dye/ink block in slot 0
            if (byPlayer.InventoryManager.ActiveHotbarSlotNumber == inkslotnumber) { return; }

            
            ItemSlot inkslot = byPlayer.InventoryManager.GetHotbarInventory()[inkslotnumber];
            int inkmat = GetInkMat(api, inkslot);
            if (inkmat == -1)
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                return;
            }
            

            Vec3i s = GetVoxelHit(blockSel);
            CuboidWithMaterial cwm = new CuboidWithMaterial();
            byte selectedmat = 0;

            foreach (uint voxint in bmb.VoxelCuboids)
            {
                //this static method converts the uint into a CubioidWithMaterial
                BlockEntityMicroBlock.FromUint(voxint, cwm);
                //if we're looking at this voxel we want to grab this material to use
                if (cwm.Contains(s.X, s.Y, s.Z)) { selectedmat = cwm.Material; break; }
            }
            //if materials paint we don't want to do anything
            if (bmb.MaterialIds[selectedmat] == inkmat) {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                return;
            }
            List<int> newmats = new List<int>(bmb.MaterialIds);
            newmats[selectedmat] = inkmat;
            bmb.MaterialIds = newmats.ToArray();
            bmb.MarkDirty(true);
            
            if (api is ICoreServerAPI && byPlayer?.WorldData.CurrentGameMode != EnumGameMode.Creative)
            {
                //Do tool wear
                int dmg = CalcDamage();
                this.DamageItem(api.World, byEntity, byPlayer.InventoryManager.ActiveHotbarSlot, dmg);
                byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
                //inventory usage
                UseInventory(api, inkslot);
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
        //crossreferences items and blocks to a relevant block to use for a material
        public static int GetInkMat(ICoreAPI api,ItemSlot forslot)
        {
            int result = -1;
            if (forslot.Itemstack == null || forslot.Itemstack.StackSize == 0) { return result; }
            if (forslot.Itemstack.Block == null||forslot.Itemstack.Block is BlockChisel) { return result; }
            if (forslot.Itemstack.Block is BlockLiquidContainerTopOpened) { return DyeCrossReference(api,forslot); }
            
            bool canChiselSet = forslot.Itemstack.Block.Attributes?["canChisel"].Exists == true;
            bool canChisel = forslot.Itemstack.Block.Attributes?["canChisel"].AsBool(false) == true;
                        
            if (canChiselSet && !canChisel) return -1;
            if (forslot.Itemstack.Block.DrawType != EnumDrawType.Cube) return -1;
            if (forslot.Itemstack.Block != null) { return forslot.Itemstack.Block.Id; }
            if (forslot.Itemstack.Block.SeasonColorMap != null || forslot.Itemstack.Block.ClimateColorMap != null) return -1;

            //Block testblock = api.World.BlockAccessor.GetBlock(new AssetLocation("game:creativeblock-18"));



            return result;
        }

        public static int DyeCrossReference(ICoreAPI api, ItemSlot forslot)
        {
            
            if (forslot == null || forslot.Itemstack == null || forslot.Itemstack.Block == null) { return -1; }
            BlockLiquidContainerTopOpened container = forslot.Itemstack.Block as BlockLiquidContainerTopOpened;
            if (container == null) { return -1; }
            ItemStack containercontents = container.GetContent(forslot.Itemstack);
            if (containercontents == null || containercontents.StackSize == 0||containercontents.Item==null) { return -1; }
            
            if (containercontents.Item.Code.ToString() == "game:dye-blue") { return api.World.GetBlock(new AssetLocation("game:creativeblock-18")).Id; }
            else if (containercontents.Item.Code.ToString() == "game:dye-red") { return api.World.GetBlock(new AssetLocation("game:creativeblock-32")).Id; }
            else if (containercontents.Item.Code.ToString() == "game:dye-yellow") { return api.World.GetBlock(new AssetLocation("game:creativeblock-36")).Id; }
            else if (containercontents.Item.Code.ToString() == "game:dye-purple") { return api.World.GetBlock(new AssetLocation("game:creativeblock-26")).Id; }
            else if (containercontents.Item.Code.ToString() == "game:dye-brown") { return api.World.GetBlock(new AssetLocation("game:creativeblock-2")).Id; }
            else if (containercontents.Item.Code.ToString() == "game:dye-green") { return api.World.GetBlock(new AssetLocation("game:creativeblock-43")).Id; }
            else if (containercontents.Item.Code.ToString() == "game:dye-orange") { return api.World.GetBlock(new AssetLocation("game:creativeblock-35")).Id; }
            else if (containercontents.Item.Code.ToString() == "game:dye-black") { return api.World.GetBlock(new AssetLocation("game:creativeblock-65")).Id; }
            else if (containercontents.Item.Code.ToString() == "game:dye-gray") { return api.World.GetBlock(new AssetLocation("game:creativeblock-72")).Id; }
            else if (containercontents.Item.Code.ToString() == "game:dye-pink") { return api.World.GetBlock(new AssetLocation("game:creativeblock-63")).Id; }
            else if (containercontents.Item.Code.ToString() == "game:dye-white") { return api.World.GetBlock(new AssetLocation("game:creativeblock-79")).Id; }

            return -1;
        }

        //returns voxel coordinates of a block selection, accounting for facing etc
        public static Vec3i GetVoxelHit(BlockSelection blockSel)
        {
            //convert the hit point into voxel coordinates
            Vec3i s = new Vec3i((int)(blockSel.HitPosition.X * 16f), (int)(blockSel.HitPosition.Y * 16f), (int)(blockSel.HitPosition.Z * 16f));
            if (blockSel.Face == BlockFacing.SOUTH) { s.Z--; }
            if (blockSel.Face == BlockFacing.UP) { s.Y--; }
            if (blockSel.Face == BlockFacing.EAST) { s.X--; }
            return s;
        }
        protected virtual int CalcDamage()
        {

            return ChiselToolLoader.serverconfig.paintBrushUseRate;
        }

        public static void UseInventory(ICoreAPI api,ItemSlot inkslot)
        {
        
            if (inkslot == null) { return; }
            if (inkslot.Itemstack == null) { return; }
            if (inkslot.Itemstack.Block == null) { return; }
            BlockLiquidContainerTopOpened container = inkslot.Itemstack.Block as BlockLiquidContainerTopOpened;
            if (container != null)
            {
                ItemStack containercontents = container.GetContent(inkslot.Itemstack);
                if (containercontents == null) { return; }
                containercontents.StackSize-=ChiselToolLoader.serverconfig.paintBrushLiquidMultiplier;
                if (containercontents.StackSize <= 0) { container.SetContent(inkslot.Itemstack,null); }
                inkslot.MarkDirty();
                return;
            }
            inkslot.Itemstack.StackSize-=1;
            if (inkslot.Itemstack.StackSize <= 0) { inkslot.Itemstack = null; }
            inkslot.MarkDirty();
        }
    }
}
