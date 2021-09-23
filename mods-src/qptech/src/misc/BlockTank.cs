using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace qptech.src
{
    public class BlockTank : BlockLiquidContainerBase, IBlockItemFlow
    {

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {

            base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);

            return true;
        }

        #region Interaction help

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            List<ItemStack> liquidContainerStacks = new List<ItemStack>();

            ItemStack[] lstacks = liquidContainerStacks.ToArray();

            foreach (CollectibleObject obj in api.World.Collectibles)
            {
                if (obj is ILiquidSource)
                {
                    List<ItemStack> stacks = obj.GetHandBookStacks((ICoreClientAPI)api);
                    if (stacks != null) liquidContainerStacks.AddRange(stacks);
                }
            }

            return new WorldInteraction[]
                    {
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-bucket-rightclick",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = lstacks
                    }
            };
        }


        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return new WorldInteraction[]
            {
                new WorldInteraction()
                {
                    ActionLangCode = "heldhelp-place",
                    HotKeyCode = "sneak",
                    MouseButton = EnumMouseButton.Right,
                    ShouldApply = (wi, bs, es) => {
                        return true;
                    }
                }
            };
        }

        #endregion

        #region Render
        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            Dictionary<int, MeshRef> meshrefs = null;
            
            object obj;
            if (capi.ObjectCache.TryGetValue("tankMeshRefs" + Variant["metal"], out obj))
            {
                meshrefs = obj as Dictionary<int, MeshRef>;
            }
            else
            {
                capi.ObjectCache["tankMeshRefs" + Variant["metal"]] = meshrefs = new Dictionary<int, MeshRef>();
            }

            ItemStack contentStack = GetContent(capi.World, itemstack);
            if (contentStack == null) return;

   
            int hashcode = GetBucketHashCode(capi.World, contentStack);
         
            MeshRef meshRef = null;


            if (!meshrefs.TryGetValue(hashcode, out meshRef))
            {
                MeshData meshdata = GenMesh(capi, contentStack);
                //meshdata.Rgba2 = null;

                meshrefs[hashcode] = meshRef = capi.Render.UploadMesh(meshdata);

            }

            renderinfo.ModelRef = meshRef;
        }



        public int GetBucketHashCode(IClientWorldAccessor world, ItemStack contentStack)
        {
            string s = contentStack.StackSize + "x" + contentStack.Collectible.Code.ToShortString();
            return s.GetHashCode();
        }



        public override void OnUnloaded(ICoreAPI api)
        {
            ICoreClientAPI capi = api as ICoreClientAPI;
            if (capi == null) return;

            object obj;
            if (capi.ObjectCache.TryGetValue("tankMeshRefs" + Variant["metal"], out obj))
            {
                Dictionary<int, MeshRef> meshrefs = obj as Dictionary<int, MeshRef>;

                foreach (var val in meshrefs)
                {
                    val.Value.Dispose();
                }

                capi.ObjectCache.Remove("tankMeshRefs" + Variant["metal"]);
            }
        }


        public MeshData GenMesh(ICoreClientAPI capi, ItemStack contentStack, BlockPos forBlockPos = null)
        {

            Shape shape = capi.Assets.TryGet("machines:shapes/block/metal/container/tank.json").ToObject<Shape>();
            MeshData bucketmesh;
            capi.Tesselator.TesselateShape(this, shape, out bucketmesh);

            if (contentStack != null)
            {
                
                WaterTightContainableProps props = GetInContainerProps(contentStack);
                ContainerTextureSource contentSource = new ContainerTextureSource(capi, contentStack, props.Texture);

                float level = contentStack.StackSize / props.ItemsPerLitre;

                MeshData contentMesh;

                if (level <= 10f % 20f)
                {
                    shape = capi.Assets.TryGet("machines:shapes/block/metal/container/" + (Variant["containers"] + "-" + 10) + ".json").ToObject<Shape>();
                }

                else if (level <= 30f % 40f)
                {
                    shape = capi.Assets.TryGet("machines:shapes/block/metal/container/" + (Variant["containers"] + "-" + 20) + ".json").ToObject<Shape>();
                }

                else if (level <= 50f % 60f)
                {
                    shape = capi.Assets.TryGet("machines:shapes/block/metal/container/" + (Variant["containers"] + "-" + 30) + ".json").ToObject<Shape>();
                }

                else if (level <= 70f % 80f)
                {
                    shape = capi.Assets.TryGet("machines:shapes/block/metal/container/" + (Variant["containers"] + "-" + 40) + ".json").ToObject<Shape>();
                }

                else if (level <= 90f % 100f)
                {
                    shape = capi.Assets.TryGet("machines:shapes/block/metal/container/" + (Variant["containers"] + "-" + 50) + ".json").ToObject<Shape>();
                }

                capi.Tesselator.TesselateShape("tank", shape, out contentMesh, contentSource, new Vec3f(Shape.rotateX, Shape.rotateY, Shape.rotateZ));


                if (props.ClimateColorMap != null)
                {
                    int col = capi.World.ApplyColorMapOnRgba(props.ClimateColorMap, null, ColorUtil.WhiteArgb, 196, 128, false);
                    if (forBlockPos != null)
                    {
                        col = capi.World.ApplyColorMapOnRgba(props.ClimateColorMap, null, ColorUtil.WhiteArgb, forBlockPos.X, forBlockPos.Y, forBlockPos.Z, false);
                    }

                    byte[] rgba = ColorUtil.ToBGRABytes(col);

                    for (int i = 0; i < contentMesh.Rgba.Length; i++)
                    {
                        contentMesh.Rgba[i] = (byte)((contentMesh.Rgba[i] * rgba[i % 4]) / 255);
                    }
                }

                for (int i = 0; i < contentMesh.Flags.Length; i++)
                {
                    contentMesh.Flags[i] = contentMesh.Flags[i] & ~(1 << 12); // Remove water waving flag
                }

                bucketmesh.AddMeshData(contentMesh);

                // Water flags
                if (forBlockPos != null)
                {
                    bucketmesh.CustomInts = new CustomMeshDataPartInt(bucketmesh.FlagsCount);
                    bucketmesh.CustomInts.Count = bucketmesh.FlagsCount;
                    bucketmesh.CustomInts.Values.Fill(0x4000000); // light foam only

                    bucketmesh.CustomFloats = new CustomMeshDataPartFloat(bucketmesh.FlagsCount * 2);
                    bucketmesh.CustomFloats.Count = bucketmesh.FlagsCount * 2;
                }
            }


            return bucketmesh;
        }

        #endregion

        #region Block interact
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;

            if (!hotbarSlot.Empty && hotbarSlot.Itemstack.Collectible.Attributes?.IsTrue("handleLiquidContainerInteract") == true)
            {
                EnumHandHandling handling = EnumHandHandling.NotHandled;
                hotbarSlot.Itemstack.Collectible.OnHeldInteractStart(hotbarSlot, byPlayer.Entity, blockSel, null, true, ref handling);
                if (handling == EnumHandHandling.PreventDefault || handling == EnumHandHandling.PreventDefaultAction) return true;
            }

            if (hotbarSlot.Empty || !(hotbarSlot.Itemstack.Collectible is ILiquidInterface)) return base.OnBlockInteractStart(world, byPlayer, blockSel);


            CollectibleObject obj = hotbarSlot.Itemstack.Collectible;

            bool singleTake = byPlayer.WorldData.EntityControls.Sneak;
            bool singlePut = byPlayer.WorldData.EntityControls.Sprint;

            if (obj is BlockTank && !singleTake)
            {
                return true;
            }

            if (obj is BlockTank && !singlePut)
            {
                return false;
            }

            if (obj is ILiquidSource && !singleTake)
            {
                int moved = TryPutContent(world, blockSel.Position, (obj as ILiquidSource).GetContent(world, hotbarSlot.Itemstack), singlePut ? 1 : 9999);

                if (moved > 0)
                {
                    (obj as ILiquidSource).TryTakeContent(world, hotbarSlot.Itemstack, moved);
                    (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

                    return true;
                }
            }

            if (obj is ILiquidSink && !singlePut)
            {
                ItemStack owncontentStack = GetContent(world, blockSel.Position);
                int moved = 0;

                if (hotbarSlot.Itemstack.StackSize == 1)
                {
                    moved = (obj as ILiquidSink).TryPutContent(world, hotbarSlot.Itemstack, owncontentStack, singleTake ? 1 : 9999);
                }
                else
                {
                    ItemStack containerStack = hotbarSlot.Itemstack.Clone();
                    containerStack.StackSize = 1;
                    moved = (obj as ILiquidSink).TryPutContent(world, containerStack, owncontentStack, singleTake ? 1 : 9999);

                    if (moved > 0)
                    {
                        hotbarSlot.TakeOut(1);
                        if (!byPlayer.InventoryManager.TryGiveItemstack(containerStack, true))
                        {
                            api.World.SpawnItemEntity(containerStack, byPlayer.Entity.SidedPos.XYZ);
                        }
                    }
                }

                if (moved > 0)
                {
                    TryTakeContent(world, blockSel.Position, moved);
                    (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                    return true;
                }
            }

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        #endregion

        #region Item Interact
        public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            if (blockSel == null || byEntity.Controls.Sneak)
            {
                base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
                return;
            }

            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;

            if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            {
                byEntity.World.BlockAccessor.MarkBlockDirty(blockSel.Position.AddCopy(blockSel.Face));
                byPlayer?.InventoryManager.ActiveHotbarSlot?.MarkDirty();
                return;
            }

            // Prevent placing on normal use
            handHandling = EnumHandHandling.PreventDefaultAction;
        }

        #endregion

        public static string PerishableInfoCompact(ICoreAPI Api, ItemSlot contentSlot, float ripenRate, bool withStackName = true)
        {
            return null;
        }

        public bool HasItemFlowConnectorAt(BlockFacing facing)
        {
            return true;
        }
    }
}