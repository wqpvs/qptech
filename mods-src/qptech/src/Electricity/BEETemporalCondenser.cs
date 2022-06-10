using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Server;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;
using qptech.src.extensions;
using System.Text.RegularExpressions;
using qptech.src.itemtransport;

namespace qptech.src
{
    /// <summary>
    /// The Temporal Condenser uses electricity and temporal instability to charge relvant materials (mainly temporal steel)
    /// </summary>
    class BEETemporalCondenser:BEElectric
    {
        float chargereq = 10000;
        ItemStack contents;
        string tmpMetal;
        int textureId;
        Shape shape;
        ITexPositionSource tmpTextureSource;
        private SimpleParticleProperties smokeParticles;
        float soundlevel = 1f;
        bool alreadyPlayedSound = false;
        bool loopsound = true;
        int soundoffdelaycounter = 0;
        ILoadedSound ambientSound;
        string runsound = "";
        public const string requiredTemporalChargeKey= "temporalCharge";
        
        public virtual float SoundLevel
        {
            get { return soundlevel; }
        }
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            tempStabilitySystem = api.ModLoader.GetModSystem<SystemTemporalStability>();
            if (Block.Attributes != null) { runsound = Block.Attributes["runsound"].AsString(runsound); }
            if (api is ICoreClientAPI) { GenMesh(); }
        }

        public override void OnTick(float par)
        {
            base.OnTick(par);
            bool processing = false;

            if (contents != null&&lastPower>=usePower) {
                float requiredcharge = contents.Collectible.Attributes[requiredTemporalChargeKey].AsFloat(0);

                float currentcharge = contents.Attributes.GetFloat(requiredTemporalChargeKey, 0);
                if (requiredcharge > 0 && currentcharge < requiredcharge)
                {
                    processing = true;
                }
            }
            ToggleAmbientSounds(processing);
            if (processing && Api is ICoreServerAPI)
            {
                TryCharge();
            }
            if (Api is ICoreClientAPI){
                
                if (processing)
                {
                    yrotspeed = 1; 
                    DoRunningParticles();
                }
                else
                {
                    yrotspeed = 0;
                }
                GenMesh(); MarkDirty(true);
            }
        }
        SystemTemporalStability tempStabilitySystem;
        public virtual void TryCharge()
        {
            if (tempStabilitySystem == null) { return; }
            float stability=tempStabilitySystem.GetTemporalStability(Pos);
            if (stability > 0.9f) { return; }
            float stabbonus = Math.Min(1, 1 - stability)*10;
            //TODO: add bonuses for nearby rifts? spawn rifts on transform?
            
            if (contents==null|| contents.StackSize == 0) { return; }
            float requiredcharge = contents.Collectible.Attributes[requiredTemporalChargeKey].AsFloat(0);
            //if this isn't a chargeable object return;
            if (requiredcharge == 0) { return; }
            string temporalTransformBlockOrItem = contents.Attributes.GetString("temporalTransformBlockOrItem", "item");
            float currentcharge = contents.Attributes.GetFloat(requiredTemporalChargeKey, 0);
            string transformsto = contents.Collectible.Attributes["temporalTransformTo"].AsString("");
            currentcharge += stabbonus / (float)contents.StackSize;

            if (currentcharge >= requiredcharge) { 
             
                int qty = contents.StackSize;
                if (temporalTransformBlockOrItem == "item")
                {
                    Item newitem = Api.World.GetItem(new AssetLocation(transformsto));
                    ItemStack newstack = new ItemStack(newitem, qty);
                    contents = newstack;
                    //TODO UPDATE RENDER MESH
                    GenMesh();
                    MarkDirty(true);
                }
                else
                {
                    Block newblock = Api.World.GetBlock(new AssetLocation(transformsto));
                    ItemStack newstack = new ItemStack(newblock, qty);
                    contents = newstack;
                    GenMesh();
                     MarkDirty(true);
                    //TODO UPDATE RENDER MESH
                }
               
            }
            //item is not charged save the value
            else
            {
                contents.Attributes.SetFloat(requiredTemporalChargeKey, currentcharge);

            }
      
        }
        public virtual void DoRunningParticles()
        {
            //Temp code for steam particles, def needs to be moved to json
            
            smokeParticles = new SimpleParticleProperties(
                    0, 6, //min qty/max qty
                    ColorUtil.ToRgba(64, 128, 255, 64), //color
                    new Vec3d(0.25, 28, 0.25), //min pos
                    new Vec3d(0.75, 32, 0.75), //max pos
                    new Vec3f(-1 / 32f, 0.2f, -1 / 32f), //min velocity
                    new Vec3f(1 / 32f, 1f, 1 / 32f), //max velocity
                    0.25f, //particle life
                    -0.025f / 4, //particle gravity
                    0.2f, //min size
                    0.6f, //max size
                    EnumParticleModel.Quad
                );

            smokeParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.25f);
            smokeParticles.SelfPropelled = true;
            smokeParticles.AddPos.Set(8 / 16.0, 0, 8 / 16.0);
            smokeParticles.MinPos.Set(Pos.X + 4 / 16f, Pos.Y + 3 / 16f, Pos.Z + 4 / 16f);
            Api.World.SpawnParticles(smokeParticles);
            
        }
        public virtual bool PlayerClicked(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            //if (capi != null) { return true; }

            //Pull an item from the players hand if applicable
            //TODO: Proper item transfer
            if ((contents == null || contents.StackSize == 0)&&(byPlayer.Entity.RightHandItemSlot.Itemstack!=null&&byPlayer.Entity.RightHandItemSlot.Itemstack.StackSize>0))
            {
                
                
                float requirecharge= byPlayer.Entity.RightHandItemSlot.Itemstack.Collectible.Attributes[requiredTemporalChargeKey].AsFloat(0);
                if (requirecharge == 0) { return false; }
                contents = new ItemStack(byPlayer.Entity.RightHandItemSlot.Itemstack.Collectible, 1);
                float copycharge = byPlayer.Entity.RightHandItemSlot.Itemstack.Attributes.GetFloat(requiredTemporalChargeKey, 0);
                if (copycharge != 0) { contents.Attributes.SetFloat(requiredTemporalChargeKey, copycharge); }
                byPlayer.Entity.RightHandItemSlot.Itemstack.StackSize--;
                if (byPlayer.Entity.RightHandItemSlot.Itemstack.StackSize == 0) { byPlayer.Entity.RightHandItemSlot.Itemstack = null; }
                byPlayer.Entity.RightHandItemSlot.MarkDirty();
                //TODO Update renderer!
                GenMesh();
                MarkDirty(true);
                return true;
            }
            //Otherwise give item to player
            else if (contents != null && contents.StackSize > 0 && byPlayer.Entity.RightHandItemSlot.Empty)
            {
                byPlayer.Entity.RightHandItemSlot.Itemstack = new ItemStack(contents.Collectible, contents.StackSize);
                float copycharge = contents.Attributes.GetFloat(requiredTemporalChargeKey, 0);
                if (copycharge != 0) { byPlayer.Entity.RightHandItemSlot.Itemstack.Attributes.SetFloat(requiredTemporalChargeKey, copycharge); }
                contents = null;
                byPlayer.Entity.RightHandItemSlot.MarkDirty();
                meshdata = null;
                GenMesh();
                MarkDirty(true);
                return true;
            }
            return true;
        }
        MeshData meshdata;
        float yrot = 0;
        float yrotspeed = 1;
        public virtual void GenMesh()
        {
            //if we trigger a genmesh at the server, need to broadcast the instruction to update to the client
            if (sapi != null)
            {
                sapi.Network.BroadcastBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, (int)enPacketIDs.GenMesh, null);
                return;
            }
            meshdata = null;
            
             //didn't feel like refactoring right now
            if (contents == null || (contents.Item == null && contents.Block == null)) { return; }


            if (contents.Class == EnumItemClass.Item)
            {
                if (contents.Item.FirstCodePart() == "ingot")
                {
                    
                    tmpMetal = contents.Collectible.LastCodePart();
                    //if we hardcode this to tmpMetal = "temporalsteel"; it will render an iron ingot
                    
                    tmpTextureSource = capi.Tesselator.GetTexSource(capi.World.GetBlock(new AssetLocation("machines:metalsheet-"+tmpMetal+"-down")));
                    shape = capi.Assets.TryGet("game:shapes/block/stone/forge/ingotpile.json").ToObject<Shape>();
                    textureId = tmpTextureSource[tmpMetal].atlasTextureId;
                    capi.Tesselator.TesselateShape("block-fcr", shape, out meshdata, this, new Vec3f(0,4,0), 0, 0, 0, contents.StackSize);
                    
                    
                }
                else if (contents.Item.FirstCodePart() == "metalplate")
                {

                    tmpMetal = contents.Collectible.LastCodePart();
                    //if we hardcode this to tmpMetal = "temporalsteel"; it will render an iron ingot

                    tmpTextureSource = capi.Tesselator.GetTexSource(capi.World.GetBlock(new AssetLocation("machines:metalsheet-temporalsteel-down")));
                    shape = capi.Assets.TryGet("game:shapes/block/stone/forge/platepile.json").ToObject<Shape>();
                    textureId = tmpTextureSource[tmpMetal].atlasTextureId;
                    capi.Tesselator.TesselateShape("block-fcr", shape, out meshdata, this, new Vec3f(0, 4, 0), 0, 0, 0, contents.StackSize);


                }
                else
                {
                    capi.Tesselator.TesselateItem(contents.Item, out meshdata);
                }
            }
            else
            {
                capi.Tesselator.TesselateBlock(contents.Block, out meshdata);
            }
            
            meshdata.Translate(new Vec3f(0, 0.25f, 0));
            meshdata.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, yrot* 0.0174533f, 0);
            yrot += yrotspeed;
        }
        public override TextureAtlasPosition this[string textureCode]
        {
            get { return tmpTextureSource["down"]; }
        }
        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            base.OnReceivedServerPacket(packetid, data);
            if (packetid == (int)enPacketIDs.GenMesh) { GenMesh(); }
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            
            if (meshdata == null||contents==null||contents.Collectible==null||contents.StackSize==0) { return base.OnTesselation(mesher, tessThreadTesselator); }
            mesher.AddMeshData(meshdata); return false;
            
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            float stability = tempStabilitySystem.GetTemporalStability(Pos);
            if (contents != null && contents.StackSize > 0)
            {
                float requiredcharge = contents.Collectible.Attributes[requiredTemporalChargeKey].AsFloat(0);
                
                float currentcharge = contents.Attributes.GetFloat(requiredTemporalChargeKey, 0);
                dsc.AppendLine("Loaded item: " + contents.Collectible.GetHeldItemName(contents));
                if (requiredcharge > 0)
                {
                    float pct = (float)Math.Ceiling(100*(currentcharge / requiredcharge) );
                    dsc.AppendLine("Charging progress " + pct + "%");
                }
                else
                {
                    dsc.AppendLine("Charging Complete!");
                }
            }
            if (stability > 0.9f) { dsc.AppendLine("Insufficent Temporal Instability ("+stability+")"); }
            else { dsc.AppendLine("Temporal Instability Adequate ("+stability+")"); }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            
            base.ToTreeAttributes(tree);
            tree.SetItemstack("contents", contents);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            contents = tree.GetItemstack("contents");
            if (contents != null)
            {
                contents.ResolveBlockOrItem(worldAccessForResolve);
            }
        }
        public void ToggleAmbientSounds(bool on)
        {
            if (Api.Side != EnumAppSide.Client) return;
            if (runsound == "" || SoundLevel == 0) { return; }
            if (on)
            {

                if (ambientSound == null || !ambientSound.IsPlaying && (!alreadyPlayedSound || loopsound))
                {
                    ambientSound = ((IClientWorldAccessor)Api.World).LoadSound(new SoundParams()
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
