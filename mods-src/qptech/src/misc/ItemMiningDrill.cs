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
using Vintagestory.API.Client;

namespace qptech.src.misc
{
    class ItemMiningDrill:Item
    {
        float nextactionat = 0;
        bool soundplayed = false;
        float actionspeed = 1f;
        float soundlevel = 1f;
        float SoundLevel => soundlevel;
        bool alreadyPlayedSound = false;
        bool loopsound = true;
        int soundoffdelaycounter = 0;
        float fuel = 100;
        public static SimpleParticleProperties myParticles = new SimpleParticleProperties(1, 1, ColorUtil.ColorFromRgba(0, 0, 0,75), new Vec3d(), new Vec3d(), new Vec3f(), new Vec3f());
        ILoadedSound ambientSound;
        string runsound = "sounds/drillloop";
        ICoreClientAPI capi;
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            //base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            nextactionat = actionspeed;
           

            handling = EnumHandHandling.Handled;
        }
        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            // - start particles
            // - first extension
            // - break top/bottom/side blocks (maybe add calculations to make it faster for less blocks
            // - second extension
            // - break back block
            // - reset
            if (blockSel == null) { return false; }
            if (fuel <= 0) { return false; }
            //if (!BlockFacing.HORIZONTALS.Contains(blockSel.Face)) { return false; } //not pointed at a block ahead, cancel
            if (secondsUsed > 0.25f && !soundplayed)
            {
                //api.World.PlaySoundAt(new AssetLocation("sounds/quarrytemp"), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, null, false, 8, 1);
                soundplayed = true;
            }
            myParticles.MinPos = byEntity.Pos.XYZ.Add(-0.25, 1, 0).Ahead(1f, byEntity.Pos.Pitch, byEntity.Pos.Yaw);
            myParticles.AddPos = new Vec3d(0.5, 0.5, 0.5);
            myParticles.MinVelocity = new Vec3f(0, -0.1f, 0);
            myParticles.AddVelocity = new Vec3f(0, 0.5f, 0);
            myParticles.LifeLength = 1F;
            myParticles.addLifeLength = 0.5f;
            myParticles.MinQuantity = 3;
            myParticles.AddQuantity = 10;
            myParticles.GravityEffect = 0F;
            //myParticles.MinSize = 0.1F;
            //myParticles.MaxSize = 1.0F;
            myParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, 2);
            myParticles.ParticleModel = EnumParticleModel.Quad;
            byEntity.World.SpawnParticles(myParticles);
            myParticles.OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -255);
            ToggleAmbientSounds(true, blockSel.Position);
            if (secondsUsed > nextactionat)
            {

                IPlayer p = api.World.NearestPlayer(byEntity.Pos.X, byEntity.Pos.Y, byEntity.Pos.Z);
                Block tb;
                
                List<BlockPos> positions = new List<BlockPos>();
                
                int sx = 0; int ex = 0;
                int sy = 0; int ey = 0;
                int sz = 0; int ez = 0;
                if (blockSel.Face == BlockFacing.UP||blockSel.Face==BlockFacing.DOWN)
                {
                    sx = -1;ex = 1;sz = -1;ez = +1;
                }
                else if (blockSel.Face == BlockFacing.EAST || blockSel.Face == BlockFacing.WEST)
                {
                    sy = -1;ey = 1;
                    sz = -1;ez = 1;
                }
                else if (blockSel.Face == BlockFacing.NORTH || blockSel.Face == BlockFacing.SOUTH)
                {
                    sy = -1;ey = 1;
                    sx = -1;ex = 1;
                }
                
                for (int xc = sx; xc < ex+1; xc++)
                {
                    for (int zc = sz; zc < ez + 1; zc++)
                    {
                        for (int yc = sy; yc < ey + 1; yc++)
                        {
                            BlockPos newpos = blockSel.Position.Copy();
                            newpos.X += xc;
                            newpos.Y += yc;
                            newpos.Z += zc;
                            positions.Add(newpos);
                        }
                    }
                }

                foreach (BlockPos bp in positions)
                {
                    
                    tb = api.World.BlockAccessor.GetBlock(bp);
                    if (tb == null) { continue; }
                    if (tb.MatterState != EnumMatterState.Solid) { continue; }
                    if (tb.RequiredMiningTier > 5) { continue; }
                    if (!api.World.Claims.TryAccess(p, bp, EnumBlockAccessFlags.BuildOrBreak)) { continue; }
                    tb.OnBlockBroken(api.World, bp, p, 1);
                
                    
                }
                nextactionat += actionspeed;
                fuel--;
            }
            return true;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            soundplayed = false;
            nextactionat = 0;
            CleanSound();
            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);
        }
        public override void OnHeldDropped(IWorldAccessor world, IPlayer byPlayer, ItemSlot slot, int quantity, ref EnumHandling handling)
        {
            base.OnHeldDropped(world, byPlayer, slot, quantity, ref handling);
            CleanSound();
        }

        public override void OnUnloaded(ICoreAPI api)
        {
            base.OnUnloaded(api);
            CleanSound();
        }

        void CleanSound()
        {
            ambientSound?.Stop();
            ambientSound?.Dispose();
            ambientSound = null;
        }

        public void ToggleAmbientSounds(bool on, BlockPos Pos)
        {
            if (api.Side != EnumAppSide.Client) return;
            if (runsound == "" || SoundLevel == 0) { return; }
            if (on)
            {

                if (ambientSound == null || !ambientSound.IsPlaying && (!alreadyPlayedSound || loopsound))
                {
                    ambientSound = ((IClientWorldAccessor)api.World).LoadSound(new SoundParams()
                    {
                        Location = new AssetLocation(runsound),
                        ShouldLoop = loopsound,
                        Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                        DisposeOnFinish = false,
                        Volume = SoundLevel,
                        Range = 10
                    });
                    soundoffdelaycounter = 0;
                    ambientSound.Start();
                    alreadyPlayedSound = true;
                }
            }
            else if (loopsound && soundoffdelaycounter < 10)
            {
                soundoffdelaycounter++;
            }
            else
            {
                ambientSound?.Stop();
                ambientSound?.Dispose();
                ambientSound = null;
                alreadyPlayedSound = false;
            }

        }
        
    }

}

