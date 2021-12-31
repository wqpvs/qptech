﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.API.Client;
using qptech.src.networks;
namespace qptech.src
{
    public class BEElectric : BlockEntity, IPowerNetworkMember, ITexPositionSource
    {
        /*base class to handle electrical devices*/
        bool showextrainfo = false; //if true will show NetworkID and MemberID in block info
        public virtual bool disableAnimations => true;
        public virtual int AvailablePower() {
            if (!isOn||!generatorready) { return 0; }
            return genPower;
        }
        bool generatorready = true;
        public virtual void SetAvailablePower(bool onoff)
        {
            generatorready = onoff;
        }
        public virtual bool IsBattery => IsOn&&fluxStorage > 0;
        public virtual int AvailableStorage() {
            if (!isOn || !IsBattery) { return 0; }
            return storedFlux;
        }
        public virtual int DrawStoredPower(int amt) {
            if (!isOn || !IsBattery) { return 0; }
            int usepower = Math.Min(amt, storedFlux);
            if (usepower < 0) { usepower = 0; return 0; }
            storedFlux -= usepower;
            MarkDirty();
            return usepower;
        }
        public virtual int RequestPower()
        {
            
            if (!isOn) { return 0; }
            return usePower;
        }
        public virtual void OnPulse(string channel)
        {

        }
        public virtual int ReceivePowerOffer(int amt)
        {
            lastPower = Math.Min(amt,usePower);MarkDirty(true);
            if (lastPower == 0)
            {
                //check
                if (1 == 1) { }
            }
            if (!isOn || lastPower < usePower) { return 0; }
            return usePower;
        }
        public virtual int StorePower(int amt)
        {
            if (!isOn || !IsBattery) { return 0; }
            int storeamt = Math.Min(amt, fluxStorage - storedFlux);
            if (storeamt < 0) { storeamt = 0; }
            storedFlux += storeamt;
            MarkDirty();
            return storeamt;
        }
        Guid networkID = Guid.Empty;
        Guid memberID = Guid.Empty;
        public Guid MemberID
        {
            get {
               
                return memberID;
            }
            
        }
        public Guid NetworkID => networkID;
        public string ProductID => "power";
        string networkstatus = "";
        public virtual void NetworkRemove()
        {
            networkID = Guid.Empty;
            //lastPower = 0;
            MarkDirty(true);
        }
        public virtual int LastPower => lastPower;
        public virtual void NetworkJoin(Guid newnetwork)
        {
            if (newnetwork == Guid.Empty) { return; }
            //if (newnetwork == NetworkID) { return; }
            FlexNetworkManager.LeaveNetwork(NetworkID, this);
            //lastPower = 0;
            bool ok=FlexNetworkManager.JoinNetworkWithID(newnetwork,this as IFlexNetworkMember);
            if (ok) { 
                networkID = newnetwork;
            }
            MarkDirty(true);
        }
        public virtual Guid GetNetworkID(BlockPos requestedby, string fortype)
        {
            if (!IsOn) { return Guid.Empty; }
            foreach (BlockFacing bf in distributionFaces)
            {
                BlockPos checkpos = Pos.AddCopy(bf);
                if (requestedby == checkpos) { return NetworkID; }
            }
            return Guid.Empty;
        }
        public TextureAtlasPosition this[string textureCode]
        {
            get
            {
                //capi.BlockTextureAtlas.Positions[Block.Textures[path].Baked.TextureSubId];
                return capi.BlockTextureAtlas.Positions[atlasBlock.Textures[UseTexture].Baked.TextureSubId];
            }
        }
        public Size2i AtlasSize => (Api as ICoreClientAPI).BlockTextureAtlas.Size;
        protected ICoreClientAPI capi;
        protected Block atlasBlock; //this block will reference a shape which will reference all used textures
        protected List<string> displayTextures; //this is a list of texture file names, must be include in the atlas block's shape
        int texno = 0; //the index number
        protected Vec3f displayOffset; //where the display will be located (if used)
        protected bool showFluxDisplay = false;
        protected float translatefactor => 16;
        protected virtual string UseTexture => displayTextures[texno];
        public virtual float DisplayPercentage => 0;
        
        protected bool isOn = true;        //if it's not on it won't do any power processing
        protected List<BlockFacing> distributionFaces; //what faces are valid for distributing power
        protected List<BlockFacing> receptionFaces; //what faces are valid for receiving power
        protected int lastPower = 0;
        protected int genPower = 0;
        protected int usePower = 0;
        protected int fluxStorage = 0;
        protected int storedFlux = 0;
        public virtual bool IsPowered { get { return IsOn && lastPower>0; } }
        public virtual bool IsOn { get { return isOn; } }
        protected bool notfirsttick = false;
        protected bool justswitched = false; //create a delay after the player switches power
        
        public BlockEntity EBlock { get { return this as BlockEntity; } }
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            //TODO need to load list of valid faces from the JSON for this stuff
            SetupIOFaces();

            
            if (Block.Attributes == null) { api.World.Logger.Error("ERROR BEE INITIALIZE HAS NO BLOCK"); return; }
            usePower = Block.Attributes["useFlux"].AsInt(usePower);
            genPower = Block.Attributes["genFlux"].AsInt(genPower);
            fluxStorage = Block.Attributes["fluxStorage"].AsInt(fluxStorage);
            RegisterGameTickListener(OnTick, 75);
            notfirsttick = false;
            if (api is ICoreClientAPI)
            {
                capi = api as ICoreClientAPI;
            }
            displayTextures = new List<string>();
            string displaytextureatlasblockname = "machines:dummygauge";
            displaytextureatlasblockname = Block.Attributes["atlasBlock"].AsString(displaytextureatlasblockname);
            atlasBlock = api.World.GetBlock(new AssetLocation(displaytextureatlasblockname));
            float[] displayoffset = Block.Attributes["displayOffset"].AsArray<float>();
            if (displayoffset == null||displayoffset.Length!=3)
            {
                displayOffset = new Vec3f(4, 10, 3);
                displayOffset = displayOffset / translatefactor;
            }
            else
            {
                displayOffset = new Vec3f(displayoffset);
                displayOffset = displayOffset / translatefactor;
            }
            string[] displaytexturelist = Block.Attributes["displayTextures"].AsArray<string>();
            if (displaytexturelist == null || displaytexturelist.Length==0)
            {
                displayTextures = new List<string>();
                displayTextures.Add("roundgauge-0");
                displayTextures.Add("roundgauge-10");
                displayTextures.Add("roundgauge-20");
                displayTextures.Add("roundgauge-30");
                displayTextures.Add("roundgauge-40");
                displayTextures.Add("roundgauge-50");
                displayTextures.Add("roundgauge-60");
                displayTextures.Add("roundgauge-70");
                displayTextures.Add("roundgauge-80");
                displayTextures.Add("roundgauge-90");
                displayTextures.Add("roundgauge-100");
            }
            else
            {
                displayTextures = displayTextures.ToList<string>();
            }
            showFluxDisplay = Block.Attributes["showFluxDisplay"].AsBool(showFluxDisplay);
            bool dirty = false;
            if (memberID == Guid.Empty && (api is ICoreServerAPI))
            {
                memberID = Guid.NewGuid();
                dirty = true;
            }
            if (NetworkID != Guid.Empty&&(api is ICoreServerAPI))
            {
                FlexNetworkManager.RecreateNetwork(NetworkID,ProductID);
                dirty = true;
            }
            if (dirty) { MarkDirty(true); }
        }

        //attempt to load power distribution and reception faces from attributes, and orient them to this blocks face if necessary
        public virtual void SetupIOFaces()
        {
            string[] cfaces = { };

            if (Block.Attributes == null)
            {
                distributionFaces = BlockFacing.HORIZONTALS.ToList<BlockFacing>();
                receptionFaces = BlockFacing.HORIZONTALS.ToList<BlockFacing>();

                return;
            }
            if (!Block.Attributes.KeyExists("receptionFaces")) { receptionFaces = BlockFacing.ALLFACES.ToList<BlockFacing>(); }
            else
            {
                cfaces = Block.Attributes["receptionFaces"].AsArray<string>(cfaces);
                receptionFaces = new List<BlockFacing>();
                foreach (string f in cfaces)
                {
                    receptionFaces.Add(OrientFace(Block.Code.ToString(), BlockFacing.FromCode(f)));
                }
            }

            if (!Block.Attributes.KeyExists("distributionFaces")) {
                distributionFaces = BlockFacing.ALLFACES.ToList<BlockFacing>();
            }
            else
            {
                cfaces = Block.Attributes["distributionFaces"].AsArray<string>(cfaces);
                distributionFaces = new List<BlockFacing>();
                foreach (string f in cfaces)
                {
                    distributionFaces.Add(OrientFace(Block.Code.ToString(), BlockFacing.FromCode(f)));
                }
            }

        }
        
        public virtual void FindConnections()
        {
           
            if (Api is ICoreClientAPI) { return; }
            
            if (NetworkID == Guid.Empty)
            {
                networkID = FlexNetworkManager.RequestNewNetwork(ProductID);
                if (networkID != Guid.Empty) { MarkDirty(); }
            }
            NetworkJoin(networkID);
            
            GrowPowerNetwork();
            
            
        }
        protected virtual void GrowPowerNetwork()
        {
            foreach (BlockFacing f in distributionFaces)//ALL FACES IS A TEMPORARY MEASURE!
            {
                BlockPos bp = Pos.Copy().Offset(f);
                BlockEntity checkblock = Api.World.BlockAccessor.GetBlockEntity(bp);
                IPowerNetworkMember pnw = checkblock as IPowerNetworkMember;
                if (pnw == null) { continue; }
                Guid othernetwork = pnw.GetNetworkID(Pos,ProductID);
                if (othernetwork == Guid.Empty) { continue; }
                if (othernetwork==NetworkID) { continue; }
                IFlexNetwork othernet = FlexNetworkManager.GetNetworkWithID(othernetwork);
                IFlexNetwork mynet = FlexNetworkManager.GetNetworkWithID(NetworkID);
                if (othernet!=null&&mynet!=null&& othernet.GetMembers().Count >= mynet.GetMembers().Count)
                {
                    NetworkJoin(pnw.NetworkID); break;
                }
                
            }
        }
        



        public virtual void CleanBlock()
        {
            
            if (Api is ICoreServerAPI)
            {
                FlexNetworkManager.DeleteNetwork(NetworkID);
            }
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            CleanBlock();
        }
        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            CleanBlock();
        }


        public virtual void OnTick(float par)
        {
            if (Api is ICoreServerAPI&&isOn)
            {
                FindConnections();
                notfirsttick = true;
            }

            if (Api is ICoreClientAPI && showFluxDisplay)
            {
                UpdateFluxDisplay();
            }
        }
        protected virtual void UpdateFluxDisplay()
        {
            float pcttracker = DisplayPercentage;
            if (pcttracker > 1) { pcttracker = 1; }
            if (pcttracker < 0) { pcttracker = 0; }
            int newtexno = (int)((float)(displayTextures.Count - 1) * pcttracker);

            if (newtexno != texno)
            {
                texno = newtexno;
                this.MarkDirty(true);
            }
        }

        protected virtual float DisplayRotation()
        {
            float rot = 0;
            switch (Block.LastCodePart())
            {
                case "east": rot = 270; break;
                case "south": rot = 180; break;
                case "west": rot = 90; break;
                case "north": rot = 0; break;
            }
            return rot;
        }
        protected virtual Vec3f DisplayOffset() //display offset but translated directionally
        {
            switch (Block.LastCodePart())
            {
                case "east": return displayOffset;
                case "south": return displayOffset;
                case "west": return displayOffset;
            }
            return displayOffset;
        }
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {

            if (!showFluxDisplay) { return base.OnTesselation(mesher, tessThreadTesselator); }
            Shape displayshape = capi.TesselatorManager.GetCachedShape(new AssetLocation("machines:block/metal/electric/roundgauge0"));


            MeshData meshdata;
            capi.Tesselator.TesselateShape("roundgauge0" + Pos.ToString(), displayshape, out meshdata, this);



            meshdata.Translate(DisplayOffset());
            meshdata.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, GameMath.DEG2RAD * DisplayRotation(), 0);


            mesher.AddMeshData(meshdata);
            return base.OnTesselation(mesher, tessThreadTesselator);
        }
        
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            
            if (networkID == Guid.Empty) { dsc.AppendLine("not connected to any network"); }
            else { 
                
                dsc.AppendLine(networkstatus);
                
            }

            if (IsBattery)
            {
                dsc.Append(" Storing " + storedFlux + " out of " + fluxStorage + " flux");
            }
            else if (genPower > 0) {
                dsc.Append(" Generates " + genPower + " flux");
                if (!isOn) { dsc.Append("(OFF)"); } 
            }
            else if (usePower > 0) {
                dsc.Append( "Uses " + usePower + " flux" );
                if (!IsOn) { dsc.Append(" (OFF) "); }
                else if (!IsPowered) {dsc.Append(" (NO POWER) "); }
                else { dsc.Append(" (OK) "); }
            }
            dsc.AppendLine("");
            if (showextrainfo)
            {
                dsc.AppendLine("MemberID  " + memberID);
                dsc.AppendLine("NetworkID " + networkID);
            }
            //dsc.AppendLine("IN:" + inputConnections.Count.ToString() + " OUT:" + outputConnections.Count.ToString());
        }

        //Used for other power devices to offer this device some energy returns how much power was used
        //API
        
        
        //Attempt to send out power (can be overridden for devices that only use power)
        

        public virtual void DoOverload()
        {
            ////BOOOOM!
            if (!IsOn) { return; }
            EnumBlastType blastType=EnumBlastType.OreBlast;
            var iswa = Api.World as IServerWorldAccessor;
            Api.World.BlockAccessor.SetBlock(0, Pos);
            if (iswa!=null)
            {
                iswa.CreateExplosion(Pos, blastType, 4, 15);
                isOn = false;
            }
            
        }

        public virtual void TogglePower()
        {
            ICoreClientAPI capi = Api as ICoreClientAPI;
            if (Api is ICoreClientAPI) {
                return;
            }
            //if (justswitched) { return; }
            isOn = !isOn;
            if (!isOn) { FlexNetworkManager.DeleteNetwork(NetworkID); }
            MarkDirty();
            //justswitched = true;
            Api.World.PlaySoundAt(new AssetLocation("sounds/electriczap"), Pos.X, Pos.Y, Pos.Z, null, false, 8, 1);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            lastPower = tree.GetInt("lastPower");
            storedFlux = tree.GetInt("storedFlux");
            string gid = "";
            gid = tree.GetString("networkID");
            networkstatus = tree.GetString("networkstatus");
            if (gid != "")
            {
                if (!Guid.TryParse(gid,out networkID)) { networkID = Guid.Empty; }
                
            }
            string mID = "";
            mID = tree.GetString("memberID");

            if (mID != "" && mID != null)
            {
                memberID = Guid.Parse(mID);
            }
            
            //else { networkID = Guid.Empty; }
            //if (type == null) type = defaultType; // No idea why. Somewhere something has no type. Probably some worldgen ruins

            isOn = tree.GetBool("isOn");
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("lastPower", lastPower);
            tree.SetInt("storedFlux", storedFlux);
            PowerNetwork pn = FlexNetworkManager.GetNetworkWithID(NetworkID) as PowerNetwork;
            if (pn == null) { networkstatus = "INVALID NETWORK"; }
            else { networkstatus = "Power: " + pn.NetworkStatus.nodes + " nodes. Load:" + pn.NetworkStatus.consumed + " Gen:" + pn.NetworkStatus.generated + " Stored:" + pn.NetworkStatus.stored; }
            tree.SetString("networkstatus", networkstatus);
            if (networkID != Guid.Empty) {
                tree.SetString("networkID", networkID.ToString());
            }
            else
            {
                tree.SetString("networkID", "");
            }
            if (memberID != Guid.Empty)
            {
                tree.SetString("memberID", memberID.ToString());
            }
            else
            {
                memberID = Guid.NewGuid();
                tree.SetString("memberID", "");
            }
            tree.SetBool("isOn", isOn);
        }
       
        //Take a block code (that ends in a cardinal direction) and a BlockFacing,
        //and rotate it, returning the appropriate blockfacing
        public static BlockFacing OrientFace(string checkBlockCode, BlockFacing toChange)
        {
            if (!toChange.IsHorizontal) { return toChange; }
            if (checkBlockCode.EndsWith("east"))
            {

                toChange = toChange.GetCW();
            }
            else if (checkBlockCode.EndsWith("south"))
            {
                toChange = toChange.GetCW().GetCW();
            }
            else if (checkBlockCode.EndsWith("west"))
            {
                toChange = toChange.GetCCW();
            }
    
            return toChange;
        }

        
    }
}
