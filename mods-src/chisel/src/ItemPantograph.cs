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

        const string copiedblockvoxelsK = "copiedblockvoxels";
        const string copiedblocknameK = "copiedblockname";
        const string copiedmaterialistK = "copiedmateriallist";
        SkillItem[] toolModes;
        WorldInteraction[] interactions;
        MeshData objectmesh;
        MeshRef objectmeshref;
        ICoreClientAPI capi;
        SkillItem changeMatItem;
        bool showcopiedshape = false;
        public enum enModes {COPY,FULLPASTE,ADDPASTE,UNDO,CHANGEMAT}
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            if (api is ICoreClientAPI) { capi = api as ICoreClientAPI; }
            toolModes = ObjectCacheUtil.GetOrCreate(api, "handplanerToolModes", () =>
            {
                SkillItem[] modes;
                
                modes = new SkillItem[5];
                modes[(int)enModes.COPY] = new SkillItem() { Code = new AssetLocation(enModes.COPY.ToString()), Name = Lang.Get("Snapshot Shape Mode") };
                modes[(int)enModes.FULLPASTE] = new SkillItem() { Code = new AssetLocation(enModes.FULLPASTE.ToString()), Name = Lang.Get("Replace Shape Mode") };
                modes[(int)enModes.ADDPASTE] = new SkillItem() { Code = new AssetLocation(enModes.ADDPASTE.ToString()), Name = Lang.Get("Add Shape Mode") };
                modes[(int)enModes.UNDO] = new SkillItem() { Code = new AssetLocation(enModes.UNDO.ToString()), Name = Lang.Get("Undo Last Block Change") };
                modes[(int)enModes.CHANGEMAT] = new SkillItem() { Code = new AssetLocation(enModes.CHANGEMAT.ToString()), Name = Lang.Get("(Creative Only)Paste Materials") };
                if (capi != null)
                {
                    modes[(int)enModes.COPY].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/takecopy.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                    modes[(int)enModes.COPY].TexturePremultipliedAlpha = false;
                    modes[(int)enModes.FULLPASTE].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/copy.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                    modes[(int)enModes.FULLPASTE].TexturePremultipliedAlpha = false;
                    modes[(int)enModes.ADDPASTE].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/add.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                    modes[(int)enModes.ADDPASTE].TexturePremultipliedAlpha = false;
                    modes[(int)enModes.UNDO].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/undo.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                    modes[(int)enModes.UNDO].TexturePremultipliedAlpha = false;
                    modes[(int)enModes.CHANGEMAT].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/paintbrush.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                    modes[(int)enModes.CHANGEMAT].TexturePremultipliedAlpha = false;
                    
                }
                
               
                return modes;
            });
            interactions = ObjectCacheUtil.GetOrCreate(api, "PantographInteractions", () =>
            {

                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "Perform Action",
                        MouseButton = EnumMouseButton.Right,

                    },
                   
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
           MakeCopy(slot, byEntity, blockSel, entitySel, ref handling); 
            handling = EnumHandHandling.PreventDefaultAction;
            api.World.PlaySoundAt(new AssetLocation("sounds/filtercopy"), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer, true, 12, 1);
           
        }
        //Vec3f displayoffset = new Vec3f(0.25f, 0, -0.25f);
        Vec3f displayoffset = new Vec3f(0, 0, 0);
        
        public virtual void MakeCopy(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            BlockEntityMicroBlock bmb = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityMicroBlock;
            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            if (bmb == null || bmb.VoxelCuboids == null || bmb.VoxelCuboids.Count == 0) { base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling); return; }
            
            
            string copiedname = bmb.BlockName;
            List<uint>copiedblockvoxels = new List<uint>(bmb.VoxelCuboids);
            List<int>copiedblockmaterials = new List<int>(bmb.MaterialIds);
            int copiedvolume = (int)(bmb.VolumeRel * 16f * 16f * 16f);
            
            if (api is ICoreServerAPI) {
                byte[]bvox = SerializerUtil.Serialize<List<uint>>(copiedblockvoxels);
                byte[] bmat = SerializerUtil.Serialize<List<int>>(copiedblockmaterials);

                slot.Itemstack.Attributes.SetBytes(copiedblockvoxelsK,bvox);
                slot.Itemstack.Attributes.SetString(copiedblocknameK, copiedname);
                slot.Itemstack.Attributes.SetBytes(copiedmaterialistK, bmat);
                slot.MarkDirty();
            }
            if (api is ICoreClientAPI && showcopiedshape)
            {
                objectmeshref?.Dispose();
                
                objectmesh = BlockEntityMicroBlock.CreateMesh(capi, copiedblockvoxels, copiedblockmaterials.ToArray() );
                MeshData myshape;
                capi.Tesselator.TesselateItem(this, out myshape);
                myshape.Scale(new Vec3f(0.5f,0.5f,0.5f),2, 2, 2);
                myshape.Translate(new Vec3f(0, -0.125f, 0));
                
                objectmesh.AddMeshData(myshape);
                objectmesh.SetTexPos(capi.ItemTextureAtlas.GetPosition(this, "metal"));
                objectmesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.5f, 0.5f, 0.5f);
                objectmesh.Translate(displayoffset);
                
                
                objectmeshref = capi.Render.UploadMesh(objectmesh);
                
            }
            api.World.PlaySoundAt(new AssetLocation("sounds/filtercopy"), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer, true, 12, 1);
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
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                return;
            }
            if (slot.Itemstack.Attributes.GetInt("toolMode", (int)enModes.COPY) == (int)enModes.COPY)
            {
                MakeCopy(slot, byEntity, blockSel, entitySel, ref handling);
                handling = EnumHandHandling.PreventDefaultAction;
                return;
            }
            else if (slot.Itemstack.Attributes.GetInt("toolMode", (int)enModes.COPY) == (int)enModes.CHANGEMAT)
            {
                PasteMaterials(slot, blockSel);
                handling = EnumHandHandling.PreventDefaultAction;
                return;
            }
            List<uint> copiedblockvoxels = GetCopiedBlockVoxels(slot);
            List<int> copiedblockmaterials = GetCopiedBlockMaterials(slot);
            
            if (copiedblockmaterials==null||copiedblockvoxels==null|| bmb == null || bmb.VoxelCuboids == null || bmb.VoxelCuboids.Count == 0) { base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);handling =EnumHandHandling.NotHandled;  return; }
            int copiedblockmaterialcount = copiedblockmaterials.Count;
            if (api.ModLoader.GetModSystem<ModSystemBlockReinforcement>()?.IsReinforced(blockSel.Position) == true)
            {
                byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
                return;
            }
            List<uint> undovoxels = new List<uint>(bmb.VoxelCuboids);
            BlockPos undoposition = blockSel.Position;
            if (api is ICoreServerAPI)
            {
                slot.Itemstack.Attributes.SetBytes("undovoxels", SerializerUtil.Serialize<List<uint>>(undovoxels));
                slot.Itemstack.Attributes.SetBlockPos("undoposition", blockSel.Position);
                slot.MarkDirty();
            }
            int changedvoxels = 0;
            int originalvolume = (int)(bmb.VolumeRel*16f*16f*16f);
            //normal copy
            if (slot.Itemstack.Attributes.GetInt("toolMode", (int)enModes.COPY)==(int)enModes.FULLPASTE)
            {
                if (bmb.MaterialIds.Length < copiedblockmaterialcount) //if we have a material mismatch just use material 0 for everything
                {

                    CuboidWithMaterial cwm = new CuboidWithMaterial();
                    BlockEntityMicroBlock.FromUint(bmb.VoxelCuboids[0], cwm);
                    byte useindex = cwm.Material;

                    bmb.VoxelCuboids = CuboidStripMaterials(copiedblockvoxels, 0);
                }
                else { bmb.VoxelCuboids = new List<uint>(copiedblockvoxels); }
                changedvoxels = originalvolume;
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
            string copyname= slot.Itemstack.Attributes.GetString("copiedblockname", "");
            if (copyname != "") {
                bmb.BlockName = copyname;
            }
            bmb.MarkDirty(true);
            api.World.PlaySoundAt(new AssetLocation("sounds/player/chalkdraw1"), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer, true, 12, 1);
            if (api is ICoreServerAPI && byPlayer?.WorldData.CurrentGameMode != EnumGameMode.Creative)
            {
                int dmg = CalcDamage(changedvoxels);
                this.DamageItem(api.World, byEntity, byPlayer.InventoryManager.ActiveHotbarSlot, dmg);
                byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
            }
            handling = EnumHandHandling.PreventDefaultAction;
        }
        protected virtual int CalcDamage(int numvoxels)
        {

            if (ChiselToolLoader.serverconfig.fixedToolWear) { return (int)ChiselToolLoader.serverconfig.pantographMinimumDamagePerOp; }
            int basedamage = (int)(ChiselToolLoader.serverconfig.pantographBaseDurabilityMultiplier * (float)numvoxels);
            
            basedamage = Math.Max(ChiselToolLoader.serverconfig.pantographMinimumDamagePerOp, basedamage);
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
                undoposition = slot.Itemstack.Attributes.GetBlockPos("undoposition", null);
                //slot.Itemstack.Attributes.GetBytes("undovoxels", SerializerUtil.Deserialize<List<uint>>(undovoxels));
                //slot.Itemstack.Attributes.SetBlockPos("undoposition", blockSel.Position);
            }
            catch
            {
                return;
            }

            if (undovoxels != null&&undoposition!=null)
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
            
            int mode= Math.Min(toolModes.Length-1, slot.Itemstack.Attributes.GetInt("toolMode"));
            

            return mode;
        }
        public override void OnUnloaded(ICoreAPI api)
        {
            for (int i = 0; toolModes != null && i < toolModes.Length; i++)
            {
                toolModes[i]?.Dispose();
            }
            objectmeshref?.Dispose();
           
        }
        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel, int toolMode)
        {
            slot.Itemstack.Attributes.SetInt("toolMode", toolMode);
            if (toolMode == (int)enModes.UNDO)
            {
                Undo(slot);
                SetToolMode(slot, byPlayer, blockSel, slot.Itemstack.Attributes.GetInt("lastToolMode",0));
            }
            else if (toolMode == (int)enModes.CHANGEMAT && byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
            {
                
                SetToolMode(slot, byPlayer, blockSel, 0);
                slot.Itemstack.Attributes.SetInt("lastToolMode", 0);
            }
            else
            {
                slot.Itemstack.Attributes.SetInt("lastToolMode", toolMode);
            }
        }

        List<uint>GetCopiedBlockVoxels(ItemSlot slot)
        {
            byte[] copvox = slot.Itemstack.Attributes.GetBytes(copiedblockvoxelsK, null);
            List<uint> copiedblockvoxels = null;
            try { copiedblockvoxels = SerializerUtil.Deserialize<List<uint>>(copvox); }
            catch { }
            return copiedblockvoxels;
        }
        
        List<int>GetCopiedBlockMaterials(ItemSlot slot)
        {
            List<int> copiedmaterials = null;
            byte[] copmat = slot.Itemstack.Attributes.GetBytes(copiedmaterialistK, null);
            try { copiedmaterials = SerializerUtil.Deserialize<List<int>>(copmat); }
            catch { }

            return copiedmaterials;
        }

        /// <summary>
        /// This will take the saved list of materials and apply it to the targeted block
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="blockSel"></param>
        void PasteMaterials(ItemSlot slot, BlockSelection blockSel)
        {
            List<int> copiedmaterials = GetCopiedBlockMaterials(slot);

            if (copiedmaterials == null) { return; }
            BlockEntityMicroBlock bmb = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityMicroBlock;
            if (bmb == null) { return; }
            //if the new list of mats is larger or the same size as target block no problem
            if (bmb.MaterialIds.Count() <= copiedmaterials.Count()) { 
                bmb.MaterialIds = copiedmaterials.ToArray();
            }
            //otherwise we'll only reset what is relevant
            else
            {
                for (int c = 0; c < copiedmaterials.Count(); c++)
                {
                    bmb.MaterialIds[c] = copiedmaterials[c];
                }
            }
            
            bmb.MarkDirty(true);
        }



        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            dsc.AppendLine("Left Click to Copy, Right Click to Paste");
            dsc.AppendLine("Copying " + inSlot.Itemstack.Attributes.GetString("copiedname","")); 
            
           
        }
        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            
            return interactions.Append(base.GetHeldInteractionHelp(inSlot));
        }

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            if (objectmeshref != null&&showcopiedshape)
            {
                renderinfo.ModelRef = objectmeshref;    
            }
            base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
        }
       
        public override void OnHeldRenderOpaque(ItemSlot inSlot, IClientPlayer byPlayer)
        {
            if (objectmesh != null&&showcopiedshape)
            {
                objectmesh.Rotate(new Vec3f(0.5f+displayoffset.X, displayoffset.Y, 0.5f+displayoffset.Z), 0, 0.025f, 0);;
                capi.Render.UpdateMesh(objectmeshref,objectmesh);
            }
            base.OnHeldRenderOpaque(inSlot, byPlayer);
        }
    }
}
