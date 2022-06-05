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
using Vintagestory.GameContent;

namespace qptech.src.misc
{
    class ItemMiningDrill:Item
    {
        float nextactionat = 0;
        bool soundplayed = false;
        float actionspeed = 0.5f;
        float soundlevel = 1f;
        float SoundLevel => soundlevel;
        bool alreadyPlayedSound = false;
        bool loopsound = true;
        int soundoffdelaycounter = 0;
        float tankcapacity = 100;
        float drillheadusepertick = 0.1f;
        float fuelusepertick = 1f;
        float startdelay = 0.1f;
        public static SimpleParticleProperties myParticles = new SimpleParticleProperties(1, 1, ColorUtil.ColorFromRgba(0, 0, 0,75), new Vec3d(), new Vec3d(), new Vec3f(), new Vec3f());
        public const string fuelattribute = "fuelintank";
        public const string drillheadattribute = "drillhead";
        public HUDMiningDrill hud;
        ILoadedSound ambientSound;
        string runsound = "sounds/drillloop";
        ICoreClientAPI capi;

        //While it's idle we'll constantly be building a list of affect blocks and hilighting htme
        public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
        {
            base.OnHeldIdle(slot, byEntity);
            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            ClearHighlights(api.World,byPlayer);
            BlockSelection blockSel = byPlayer.CurrentBlockSelection;
            if (blockSel == null) { return; }
            
            List<BlockPos> blocks = GetCurrentBlockList(blockSel,slot);
            if (blocks == null || blocks.Count == 0) { return; }
            List<int> colors = new List<int>();
            for (int c = 0; c < blocks.Count; c++){
                if (CanMine(api, blocks[c]))
                {
                    colors.Add(ColorUtil.ColorFromRgba(255, 255, 0, 128));
                }
                else
                {
                    colors.Add(ColorUtil.ColorFromRgba(255, 0, 0, 64));
                }
            }
            api.World.HighlightBlocks(byPlayer, HighlightSlotId, blocks,colors);
        }
        
        

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            //base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            nextactionat = actionspeed;
           

            handling = EnumHandHandling.Handled;
        }
        
        /// <summary>
        /// Returns a list of Block Positions based on a block selection and the current selected mining pattern
        /// </summary>
        /// <param name="blockSel">The Players' Block Selection</param>
        /// <param name="slot">The Slot the Mining Drill is in</param>
        /// <returns></returns>
        public virtual List<BlockPos> GetCurrentBlockList(BlockSelection blockSel, ItemSlot slot)
        {
            

            List<BlockPos> positions = new List<BlockPos>();

            int sx = 0; int ex = 0;
            int sy = 0; int ey = 0;
            int sz = 0; int ez = 0;
            enModes currentmode = (enModes)slot.Itemstack.Attributes.GetInt("toolMode");

            if (currentmode == enModes.DrillX)
            {
                if (blockSel.Face.IsVertical)
                {
                    //101
                    //010
                    //101
                    BlockPos newpos = blockSel.Position.Copy();
                    positions.Add(newpos);
                    newpos = newpos.Copy();
                    newpos.X--;
                    newpos.Z--;
                    positions.Add(newpos);
                    newpos = newpos.Copy();
                    newpos.X += 2;
                    positions.Add(newpos);
                    newpos = newpos.Copy();
                    newpos.Z += 2;
                    positions.Add(newpos);
                    newpos = newpos.Copy();
                    newpos.X -= 2;
                    positions.Add(newpos);

                }
                else if (blockSel.Face.IsAxisWE)
                {
                    BlockPos newpos = blockSel.Position.Copy();
                    positions.Add(newpos);
                    newpos = newpos.Copy();
                    newpos.Y--;
                    newpos.Z--;
                    positions.Add(newpos);
                    newpos = newpos.Copy();
                    newpos.Y += 2;
                    positions.Add(newpos);
                    newpos = newpos.Copy();
                    newpos.Z += 2;
                    positions.Add(newpos);
                    newpos = newpos.Copy();
                    newpos.Y -= 2;
                    positions.Add(newpos);

                }
                else if (blockSel.Face.IsAxisNS)
                {
                    BlockPos newpos = blockSel.Position.Copy();
                    positions.Add(newpos);
                    newpos = newpos.Copy();
                    newpos.Y--;
                    newpos.X--;
                    positions.Add(newpos);
                    newpos = newpos.Copy();
                    newpos.Y += 2;
                    positions.Add(newpos);
                    newpos = newpos.Copy();
                    newpos.X += 2;
                    positions.Add(newpos);
                    newpos = newpos.Copy();
                    newpos.Y -= 2;
                    positions.Add(newpos);

                }
            }
            else if (currentmode == enModes.DrillPLUS)
            {
                if (blockSel.Face.IsVertical)
                {
                    //010
                    //101
                    //010
                    BlockPos newpos = blockSel.Position.Copy();
                    newpos.Z--;
                    positions.Add(newpos);
                    newpos = newpos.Copy();
                    newpos.Z += 2;
                    positions.Add(newpos);
                    newpos = blockSel.Position.Copy();
                    newpos.X--;
                    positions.Add(newpos);
                    newpos = newpos.Copy();
                    newpos.X += 2;
                    positions.Add(newpos);
                }
                else if (blockSel.Face.IsAxisWE)
                {
                    BlockPos newpos = blockSel.Position.Copy();
                    newpos.Z--;
                    positions.Add(newpos);
                    newpos = newpos.Copy();
                    newpos.Z += 2;
                    positions.Add(newpos);
                    newpos = blockSel.Position.Copy();
                    newpos.Y--;
                    positions.Add(newpos);
                    newpos = newpos.Copy();
                    newpos.Y += 2;
                    positions.Add(newpos);

                }
                else if (blockSel.Face.IsAxisNS)
                {
                    BlockPos newpos = blockSel.Position.Copy();
                    newpos.Y--;
                    positions.Add(newpos);
                    newpos = newpos.Copy();
                    newpos.Y += 2;
                    positions.Add(newpos);
                    newpos = blockSel.Position.Copy();
                    newpos.X--;
                    positions.Add(newpos);
                    newpos = newpos.Copy();
                    newpos.X += 2;
                    positions.Add(newpos);

                }
            }
            else if (currentmode == enModes.Drill1x1)
            {
                positions.Add(blockSel.Position);
            }
            else
            {
                if (blockSel.Face.IsVertical) //special case need to also consider player facing
                {

                    if (currentmode == enModes.Drill2x1) { sy = -1; ey = 0; }
                    else if (currentmode == enModes.Drill3x1) { sy = -2; ey = 0; }
                    if (currentmode == enModes.Drill3x3) { sx = -1; ex = 1; sz = -1; ez = 1; }
                }
                else if (blockSel.Face == BlockFacing.EAST || blockSel.Face == BlockFacing.WEST)
                {
                    sy = -1; ey = 1;
                    sz = -1; ez = 1;
                    if (currentmode == enModes.Drill2x1) { sz = 0; ez = 0; sy = 0; }
                    else if (currentmode == enModes.Drill3x1) { sz = 0; ez = 0; }
                }
                else if (blockSel.Face == BlockFacing.NORTH || blockSel.Face == BlockFacing.SOUTH)
                {
                    sy = -1; ey = 1;
                    sx = -1; ex = 1;
                    if (currentmode == enModes.Drill2x1) { sx = 0; ex = 0; sy = 0; }
                    else if (currentmode == enModes.Drill3x1) { sx = 0; ex = 0; }
                }
                for (int xc = sx; xc < ex + 1; xc++)
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

            }
            return positions;
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
            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            bool creative = byPlayer?.WorldData.CurrentGameMode == EnumGameMode.Creative;
            float fuel = slot.Itemstack.Attributes.GetFloat(fuelattribute, 0);
            float drill = slot.Itemstack.Attributes.GetFloat(drillheadattribute, 100);
            if (drill <= 0&&!creative) { return false; }
            if (capi == null && fuel<tankcapacity) {
                BlockEntityContainer bec = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityContainer;
                if (bec != null) { TryFuel(bec,slot); }
            }
            if (fuel <= 0&&!creative) { return false; }
            //if (!BlockFacing.HORIZONTALS.Contains(blockSel.Face)) { return false; } //not pointed at a block ahead, cancel
            if (secondsUsed > startdelay && !soundplayed)
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
            
            myParticles.Color = ColorUtil.ToRgba(16, 16, 0, 32);
            myParticles.OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -255);
            ToggleAmbientSounds(true, blockSel.Position);
            if (secondsUsed > nextactionat)
            {

                Block tb;
                List<BlockPos> positions = GetCurrentBlockList(blockSel, slot);
  
                foreach (BlockPos bp in positions)
                {
                    
                    tb = api.World.BlockAccessor.GetBlock(bp);
                    if (tb == null) { continue; }
                    if (tb.MatterState != EnumMatterState.Solid) { continue; }
                    if (tb.RequiredMiningTier > 5) { continue; }
                    if (!CanMine(tb)) { continue; }
                    if (!api.World.Claims.TryAccess(byPlayer, bp, EnumBlockAccessFlags.BuildOrBreak)) { continue; }
                    api.World.BlockAccessor.BreakBlock(bp, byPlayer);
                    if (!creative)
                    {
                        drill -= drillheadusepertick;
                        if (drill <= 0) { break; }
                    }

                }
                nextactionat += actionspeed;
                if (!creative) { fuel -= fuelusepertick; }
                
                if (!(api is ICoreClientAPI))
                {
                    slot.Itemstack.Attributes.SetFloat(fuelattribute, fuel);
                    slot.Itemstack.Attributes.SetFloat(drillheadattribute, drill);
                    slot.MarkDirty();
                }
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
            ClearHighlights(world, byPlayer);
            CleanSound();
        }
        
        
        //return true/false if the drill can mine the given block
        public virtual bool CanMine(ICoreAPI api,BlockPos atpos)
        {
            Block tb = api.World.BlockAccessor.GetBlock(atpos);
            return CanMine(tb);
            
        }

        public virtual bool CanMine(Block tryblock)
        {
            if (tryblock.BlockMaterial != EnumBlockMaterial.Stone && tryblock.BlockMaterial != EnumBlockMaterial.Ore) { return false; }
            return true;
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

        void TryFuel(BlockEntityContainer bec, ItemSlot myslot)
        {
            if (bec == null || capi != null) { return; }
            if (bec.Inventory==null|| bec.Inventory.Empty) { return; }
            foreach (ItemSlot slot in bec.Inventory)
            {
                if (slot == null || slot.Empty || slot.StackSize<=0) { continue; }
                if (slot.Itemstack.Item == null) { continue; }
                if (!IsFuel(slot.Itemstack.Item)) { continue; }

                float fuel = slot.Itemstack.Attributes.GetFloat(fuelattribute, 0);
                float needtofill = tankcapacity - fuel;
                
                float useamt = Math.Min( needtofill ,slot.StackSize);
                if ((int)useamt > 1)
                {
                    slot.Itemstack.StackSize -= (int)Math.Ceiling(useamt);
                    if (slot.Itemstack.StackSize <= 0) { slot.Itemstack = null; }
                    fuel += useamt;
                    slot.MarkDirty();
                    bec.MarkDirty(true);
                    myslot.Itemstack.Attributes.SetFloat(fuelattribute, fuel);
                    myslot.MarkDirty();
                }
                
            }
        }

        public virtual bool IsFuel(Item checkitem)
        {
            if (checkitem.Code.ToString().Contains("spiritportion")) { return true; }
            return false;

        }

        SkillItem[] toolModes;
        WorldInteraction[] interactions;
        //TODO add X pattern
        public enum enModes { Drill1x1,Drill2x1,Drill3x1,Drill3x3,DrillX,DrillPLUS }
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            if (api is ICoreClientAPI) { capi = api as ICoreClientAPI; }
            toolModes = ObjectCacheUtil.GetOrCreate(api, "DrillToolModes", () =>
            {
                SkillItem[] modes;

                modes = new SkillItem[Enum.GetNames(typeof(enModes)).Length];
                modes[(int)enModes.Drill1x1] = new SkillItem() { Code = new AssetLocation(enModes.Drill1x1.ToString()), Name = Lang.Get("Drill 1x1") };
                modes[(int)enModes.Drill2x1] = new SkillItem() { Code = new AssetLocation(enModes.Drill2x1.ToString()), Name = Lang.Get("Drill 2x1") };
                
                modes[(int)enModes.Drill3x1] = new SkillItem() { Code = new AssetLocation(enModes.Drill3x1.ToString()), Name = Lang.Get("Drill 3x1") };
                modes[(int)enModes.Drill3x3] = new SkillItem() { Code = new AssetLocation(enModes.Drill3x3.ToString()), Name = Lang.Get("Drill 3x3") };
                modes[(int)enModes.DrillX] = new SkillItem() { Code = new AssetLocation(enModes.DrillX.ToString()), Name = Lang.Get("Drill X") };
                modes[(int)enModes.DrillPLUS] = new SkillItem() { Code = new AssetLocation(enModes.DrillPLUS.ToString()), Name = Lang.Get("Drill +") };
                if (capi != null)
                {
                    modes[(int)enModes.Drill1x1].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/drill1x1.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                    modes[(int)enModes.Drill1x1].TexturePremultipliedAlpha = false;
                    modes[(int)enModes.Drill2x1].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/drill2x1.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                    modes[(int)enModes.Drill2x1].TexturePremultipliedAlpha = false;
                    
                    modes[(int)enModes.Drill3x1].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/drill3x1.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                    modes[(int)enModes.Drill3x1].TexturePremultipliedAlpha = false;
                    modes[(int)enModes.Drill3x3].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/drill3x3.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                    modes[(int)enModes.Drill3x3].TexturePremultipliedAlpha = false;
                    modes[(int)enModes.DrillX].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/drillx.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                    modes[(int)enModes.DrillX].TexturePremultipliedAlpha = false;
                    modes[(int)enModes.DrillPLUS].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/drillplus.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                    modes[(int)enModes.DrillPLUS].TexturePremultipliedAlpha = false;
                }


                return modes;
            });
            interactions = ObjectCacheUtil.GetOrCreate(api, "DrillInteractions", () =>
            {

                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "Use Drill",
                        MouseButton = EnumMouseButton.Right,

                    },
                    
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
            var mouseslot = byPlayer.InventoryManager.MouseItemSlot;

            if (!mouseslot.Empty && mouseslot.Itemstack.Item != null )
            {
                if (mouseslot.Itemstack.Item == api.World.GetItem(new AssetLocation("machines:drillhead-steel")))
                {
                    var stack = mouseslot.TakeOut(1);
                    mouseslot.MarkDirty();
                    slot.Itemstack.Attributes.SetFloat(drillheadattribute, 100);
                    slot.MarkDirty();
                    PlaySound(api, "sounds/mechhammer", byPlayer.Entity.Pos.AsBlockPos);
                    return;
                }
               
            }
            else if (!mouseslot.Empty && mouseslot.Itemstack.Block != null)
            {
                  //eventually will be able to fill fuel here
            }
            slot.Itemstack.Attributes.SetInt("toolMode", toolMode);
        }
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            float fuel = inSlot.Itemstack.Attributes.GetFloat(fuelattribute, 0);
            float drill = inSlot.Itemstack.Attributes.GetFloat(drillheadattribute, 100);
            if (fuel <= 0)
            {
                dsc.Append("[NO FUEL!]");
            }
            else
            {
                dsc.Append("[FUEL " + Math.Ceiling(fuel / tankcapacity * 100) + "%]");
            }
            if (drill <= 0)
            {
                dsc.Append("[MISSING/BROKEN DRILLHEAD!]");
            }
            else
            {
                dsc.Append("[DRILLHEAD " + Math.Ceiling(drill) + "%]");
            }
        }
        public static int HighlightSlotId = 23;
        public void ClearHighlights(IWorldAccessor world, IPlayer player)
        {
            world.HighlightBlocks(player, HighlightSlotId, new List<BlockPos>(), new List<int>());
        }
        public static void PlaySound(ICoreAPI Api, string soundname, BlockPos pos)
        {
            if ((Api is ICoreClientAPI))
            {
                ILoadedSound pambientSound = ((IClientWorldAccessor)Api.World).LoadSound(new SoundParams()
                {
                    Location = new AssetLocation(soundname),
                    ShouldLoop = false,
                    Position = pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                    DisposeOnFinish = true,
                    Volume = 2,
                    Range = 15
                });

                pambientSound.Start();
            }
            
        }
    }

}

