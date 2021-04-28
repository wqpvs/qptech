﻿using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace QptechFurniture.src
{
    public class BlockCampFire : Block
    {
        public int Stage
        {
            get
            {
                switch (LastCodePart())
                {
                    case "construct1":
                        return 1;
                    case "construct2":
                        return 2;
                    case "construct3":
                        return 3;
                    case "construct4":
                        return 4;
                }
                return 5;
            }
        }

        public string NextStageCodePart
        {
            get
            {
                switch (LastCodePart())
                {
                    case "construct1":
                        return "construct2";
                    case "construct2":
                        return "construct3";
                    case "construct3":
                        return "construct4";
                    case "construct4":
                        return "cold";
                }
                return "cold";
            }
        }


        public bool IsExtinct;

        AdvancedParticleProperties[] ringParticles;
        Vec3f[] basePos;
        WorldInteraction[] interactions;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            IsExtinct = LastCodePart() != "lit";

            if (!IsExtinct && api.Side == EnumAppSide.Client)
            {
                ringParticles = new AdvancedParticleProperties[this.ParticleProperties.Length * 4];
                basePos = new Vec3f[ringParticles.Length];

                Cuboidf[] spawnBoxes = new Cuboidf[]
                {
                    new Cuboidf(x1: 0.125f, y1: 0, z1: 0.125f, x2: 0.3125f, y2: 0.5f, z2: 0.875f),
                    new Cuboidf(x1: 0.7125f, y1: 0, z1: 0.125f, x2: 0.875f, y2: 0.5f, z2: 0.875f),
                    new Cuboidf(x1: 0.125f, y1: 0, z1: 0.125f, x2: 0.875f, y2: 0.5f, z2: 0.3125f),
                    new Cuboidf(x1: 0.125f, y1: 0, z1: 0.7125f, x2: 0.875f, y2: 0.5f, z2: 0.875f)
                };

                for (int i = 0; i < ParticleProperties.Length; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        AdvancedParticleProperties props = ParticleProperties[i].Clone();

                        Cuboidf box = spawnBoxes[j];
                        basePos[i * 4 + j] = new Vec3f(0, 0, 0);

                        props.PosOffset[0].avg = box.MidX;
                        props.PosOffset[0].var = box.Width / 2;

                        props.PosOffset[1].avg = 0.1f;
                        props.PosOffset[1].var = 0.05f;

                        props.PosOffset[2].avg = box.MidZ;
                        props.PosOffset[2].var = box.Length / 2;

                        props.Quantity.avg /= 4f;
                        props.Quantity.var /= 4f;

                        ringParticles[i * 4 + j] = props;
                    }
                }
            }


            interactions = ObjectCacheUtil.GetOrCreate(api, "campefireInteractions-" + Stage, () =>
            {
                List<ItemStack> canIgniteStacks = new List<ItemStack>();

                foreach (CollectibleObject obj in api.World.Collectibles)
                {
                    string firstCodePart = obj.FirstCodePart();

                    if (obj is Block && (obj as Block).HasBehavior<BlockBehaviorCanIgnite>() || obj is ItemFirestarter)
                    {
                        List<ItemStack> stacks = obj.GetHandBookStacks(api as ICoreClientAPI);
                        if (stacks != null) canIgniteStacks.AddRange(stacks);
                    }
                }

                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-firepit-open",
                        MouseButton = EnumMouseButton.Right,
                        ShouldApply = (WorldInteraction wi, BlockSelection blockSelection, EntitySelection entitySelection) =>
                        {
                            return Stage == 5;
                        }
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-firepit-ignite",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCode = "sneak",
                        Itemstacks = canIgniteStacks.ToArray(),
                        GetMatchingStacks = (wi, bs, es) => {
                            BlockEntityCampFire bef = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityCampFire;
                            if (bef?.fuelSlot != null && !bef.fuelSlot.Empty && !bef.IsBurning)
                            {
                                return wi.Itemstacks;
                            }
                            return null;
                        }
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-firepit-refuel",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCode = "sneak"
                    }
                };
            });
        }


        public override EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
        {
            BlockEntityCampFire bef = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityCampFire;
            if (bef != null && bef.fuelSlot.Empty) return EnumIgniteState.NotIgnitablePreventDefault;
            if (bef != null && bef.IsBurning) return EnumIgniteState.NotIgnitablePreventDefault;

            return secondsIgniting > 3 ? EnumIgniteState.IgniteNow : EnumIgniteState.Ignitable;
        }

        public override void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
        {
            BlockEntityCampFire bef = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityCampFire;
            if (bef != null && !bef.canIgniteFuel)
            {
                bef.canIgniteFuel = true;
                bef.extinguishedTotalHours = api.World.Calendar.TotalHours;
            }

            handling = EnumHandling.PreventDefault;
        }


        public override bool ShouldReceiveClientParticleTicks(IWorldAccessor world, IPlayer player, BlockPos pos, out bool isWindAffected)
        {
            bool val = base.ShouldReceiveClientParticleTicks(world, player, pos, out _);
            isWindAffected = true;

            return val;
        }

        public override void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
        {
            if (IsExtinct)
            {
                base.OnAsyncClientParticleTick(manager, pos, windAffectednessAtPos, secondsTicking);
                return;
            }

            BlockEntityCampFire bef = manager.BlockAccess.GetBlockEntity(pos) as BlockEntityCampFire;
            if (bef != null && bef.CurrentModel == EnumCampFireModel.Over)
            {
                for (int i = 0; i < ringParticles.Length; i++)
                {
                    AdvancedParticleProperties bps = ringParticles[i];
                    bps.WindAffectednesAtPos = windAffectednessAtPos;
                    bps.basePos.X = pos.X + basePos[i].X;
                    bps.basePos.Y = pos.Y + basePos[i].Y;
                    bps.basePos.Z = pos.Z + basePos[i].Z;

                    manager.Spawn(bps);
                }

                return;
            }

            base.OnAsyncClientParticleTick(manager, pos, windAffectednessAtPos, secondsTicking);
        }


        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            int stage = Stage;
            ItemStack stack = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack;

            if (stage == 5)
            {
                BlockEntityCampFire bef = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityCampFire;

                if (bef != null && stack?.Block != null && stack.Block.HasBehavior<BlockBehaviorCanIgnite>())
                {
                    return false;
                }

                if (bef != null && stack != null && byPlayer.Entity.Controls.Sneak)
                {
                    if (stack.Collectible.CombustibleProps != null && stack.Collectible.CombustibleProps.MeltingPoint > 0)
                    {
                        ItemStackMoveOperation op = new ItemStackMoveOperation(world, EnumMouseButton.Button1, 0, EnumMergePriority.DirectMerge, 1);
                        byPlayer.InventoryManager.ActiveHotbarSlot.TryPutInto(bef.inputSlot, ref op);
                        if (op.MovedQuantity > 0)
                        {
                            (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                            return true;
                        }
                    }

                    if (stack.Collectible.CombustibleProps != null && stack.Collectible.CombustibleProps.BurnTemperature > 0)
                    {
                        ItemStackMoveOperation op = new ItemStackMoveOperation(world, EnumMouseButton.Button1, 0, EnumMergePriority.DirectMerge, 1);
                        byPlayer.InventoryManager.ActiveHotbarSlot.TryPutInto(bef.fuelSlot, ref op);
                        if (op.MovedQuantity > 0)
                        {
                            (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                            return true;
                        }
                    }
                }
                return base.OnBlockInteractStart(world, byPlayer, blockSel);
            }


            if (stack != null && TryConstruct(world, blockSel.Position, stack.Collectible, byPlayer))
            {
                if (byPlayer != null && byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
                {
                    byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(1);
                }
                return true;
            }


            return false;
        }

        public bool TryConstruct(IWorldAccessor world, BlockPos pos, CollectibleObject obj, IPlayer player)
        {
            int stage = Stage;

            if (obj.Attributes?.IsTrue("firepitConstructable") != true) return false;

            if (stage == 5) return false;

            if (stage == 4 && world.BlockAccessor.GetBlock(pos.DownCopy()).Code.Path.Equals("firewoodpile"))
            {
                Block charcoalPitBlock = world.GetBlock(new AssetLocation("charcoalpit"));
                if (charcoalPitBlock != null)
                {
                    world.BlockAccessor.SetBlock(charcoalPitBlock.BlockId, pos);

                    BlockEntityCharcoalPit be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityCharcoalPit;
                    be?.Initialize((ICoreAPI)player);

                    (player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

                    return true;
                }
            }

            Block block = world.GetBlock(CodeWithParts(NextStageCodePart));
            world.BlockAccessor.ExchangeBlock(block.BlockId, pos);
            world.BlockAccessor.MarkBlockDirty(pos);
            if (block.Sounds != null) world.PlaySoundAt(block.Sounds.Place, pos.X, pos.Y, pos.Z, player);

            if (stage == 4)
            {
                BlockEntity be = world.BlockAccessor.GetBlockEntity(pos);
                if (be is BlockEntityCampFire)
                {
                    ((BlockEntityCampFire)be).inventory[0].Itemstack = new ItemStack(obj, 4);
                }
            }

            (player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

            return true;
        }


        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }
    }
}