using System;
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
        bool acceptsdirectpower = true;
        WireRenderer wirerenderer;
        public virtual bool AcceptsDirectPower => acceptsdirectpower;
        public virtual bool showToggleButton => false;
        public virtual bool disableAnimations => true;
        List<BlockPos> directlinks;
        public List<BlockPos> DirectLinks => directlinks;
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
        public Vec3f wireoffset;
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
            acceptsdirectpower = Block.Attributes["acceptsdirectpower"].AsBool(acceptsdirectpower);

            float[] wireoffseta = Block.Attributes["wireoffset"].AsArray<float>();
            if (wireoffseta != null )
            {
                wireoffset = new Vec3f(wireoffseta[0], wireoffseta[1], wireoffseta[2]);
            }
            else
            {
                wireoffset = new Vec3f(0.5f, 0.5f, 0.5f);
            }
            if (api is ICoreClientAPI && acceptsdirectpower)
            {

                capi = api as ICoreClientAPI;
                capi.Event.RegisterRenderer(wirerenderer = new WireRenderer(Pos, capi), EnumRenderStage.Opaque, "wire");
                wirerenderer.TextureName= new AssetLocation("machines:block/rubber/cable.png");
                wirerenderer.bee = this;
                wirerenderer.wireoffset = wireoffset;
            }
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

            showFluxDisplay = false;
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
            /*if (Block.HasBehavior<BlockBehaviorCanAttach>()){
                ClothManager cm = Api.ModLoader.GetModSystem<ClothManager>();
                ClothSystem cs = cm.GetClothSystemAttachedToBlock(Pos);
                if (cs == null) { return; }
                ClothPoint fp = cs.FirstPoint;
                ClothPoint lp = cs.LastPoint;
                BlockPos otherbp;
                if (fp==null || lp == null || fp.PinnedToBlockPos==null || lp.PinnedToBlockPos==null) { return; }

                if (fp.PinnedToBlockPos == Pos) { otherbp = lp.PinnedToBlockPos; }
                else { otherbp = fp.PinnedToBlockPos; }
                BlockEntity checkblock = Api.World.BlockAccessor.GetBlockEntity(otherbp);
                IPowerNetworkMember pnw = checkblock as IPowerNetworkMember;
                if (pnw == null) { return; }
                Guid othernetwork = pnw.NetworkID;
                
                if (othernetwork == Guid.Empty) { return; }
                if (othernetwork == NetworkID) { return; }
                IFlexNetwork othernet = FlexNetworkManager.GetNetworkWithID(othernetwork);
                IFlexNetwork mynet = FlexNetworkManager.GetNetworkWithID(NetworkID);
                if (othernet != null && mynet != null && othernet.GetMembers().Count >= mynet.GetMembers().Count)
                {
                    NetworkJoin(pnw.NetworkID); return;
                }

            }*/
            //special links for wireless power, rendered power lines etc
            
            if (DirectLinks != null && DirectLinks.Count > 0)
            {
                List<BlockPos> removestaleconnections = new List<BlockPos>();
                bool anychange = false;
                foreach (BlockPos otherbp in DirectLinks)
                {
                    BlockEntity checkblock = Api.World.BlockAccessor.GetBlockEntity(otherbp);
                    IPowerNetworkMember pnw = checkblock as IPowerNetworkMember;
                    if (pnw == null) { removestaleconnections.Add(otherbp); anychange = true; continue; }
                    Guid othernetwork = pnw.NetworkID;

                    if (othernetwork == Guid.Empty) { continue; }
                    if (othernetwork == NetworkID) { continue; }
                    IFlexNetwork othernet = FlexNetworkManager.GetNetworkWithID(othernetwork);
                    IFlexNetwork mynet = FlexNetworkManager.GetNetworkWithID(NetworkID);
                    //if (othernet != null && mynet != null && othernet.GetMembers().Count >= mynet.GetMembers().Count)
                    //{
                    //    NetworkJoin(pnw.NetworkID); anychange = true;
                    //}
                    if (othernet != null) { NetworkJoin(pnw.NetworkID); anychange = true; }
                }
                //Torn on this - if I don't remove them the wires render and look odd, but then there's no chance of relinking
                /*foreach (BlockPos remove in removestaleconnections)
                {
                    DirectLinks.Remove(remove);
                }*/
                if (anychange) { MarkDirty(true); }
            }
        }
        
        public virtual bool OnPowerLink(BlockPos connecttopos)
        {
            if (!AcceptsDirectPower) { return false; }
            if (DirectLinks == null) { directlinks = new List<BlockPos>(); }
            if (connecttopos == Pos) { directlinks = new List<BlockPos>(); return true; }
            if (directlinks.Contains(connecttopos)) { return false; }
            directlinks.Clear();
            directlinks.Add(connecttopos);
            MarkDirty();
            return true;
        }

        public static BlockPos startlink;
        public virtual bool OnWireClick(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (Api is ICoreServerAPI) { return false; }
            if (!AcceptsDirectPower) { return false; }
            if (startlink == null) { startlink = blockSel.Position; }
            else 
            {
                byte[] data;
                data = SerializerUtil.Serialize<BlockPos>(startlink);
                (Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, (int)enPacketIDs.Wire, data);
                
                startlink = null;
            }
            
            return true;
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
            wirerenderer?.Dispose();
            wirerenderer = null;
            CleanBlock();
        }
        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            wirerenderer?.Dispose();
            wirerenderer = null;
            CleanBlock();
        }



        public virtual void OnTick(float par)
        {
            if (Api is ICoreServerAPI&&isOn)
            {
                FindConnections();
                notfirsttick = true;
            }

            if (Api is ICoreClientAPI )
            {
                if (wirerenderer != null)
                {
                    wirerenderer.GenModel();
                }
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
            if (DirectLinks != null && directlinks.Count > 0)
            {
                dsc.AppendLine("Direct Power links to");
                foreach (BlockPos bp in DirectLinks)
                {
                    dsc.AppendLine(bp.ToString());
                }
            }
            if (startlink != null)
            {
                dsc.AppendLine("Wire click will link to " + startlink.ToString());
            }
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
        public enum enPacketIDs
        {
            SetOrder = 99990001,
            TogglePower = 99990002,
            ToggleMode = 99990003,
            Halt = 99990004,
            Wrench = 99990005,
            Wire = 99990006
        }
        public virtual void Wrench()
        {
            if (Api is ICoreClientAPI)
            {

                (Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, (int)enPacketIDs.Wrench, null);
            }
        }

        public virtual void TogglePowerButton()
        {
            if (Api is ICoreClientAPI)
            {
                
                (Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, (int)enPacketIDs.TogglePower, null);
            }
            else
            {
                TogglePower();
            }
        }
        public virtual void ToggleMode()
        {
            if (Api is ICoreClientAPI)
            {

                (Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, (int)enPacketIDs.ToggleMode, null);
            }
            
        }
        public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
        {
            if (packetid == (int)enPacketIDs.TogglePower)
            {
                TogglePower();
            }
            else if (packetid == (int)enPacketIDs.Wrench)
            {
                Wrench();
            }
            else if (packetid == (int)enPacketIDs.Wire)
            {
                if (data != null)
                {
                    BlockPos tolink = SerializerUtil.Deserialize<BlockPos>(data);
                    if (tolink != null)
                    {
                        bool trylink=OnPowerLink(tolink);
                        if (trylink&&fromPlayer!=null&&fromPlayer.Entity!=null&&fromPlayer.Entity.RightHandItemSlot!=null)
                        {
                            
                            ItemStack phand = fromPlayer.Entity.RightHandItemSlot.Itemstack;
                            if (fromPlayer.Entity.RightHandItemSlot.Itemstack == null || fromPlayer.Entity.RightHandItemSlot.Itemstack.StackSize == 0) { return; }
                            if (fromPlayer.Entity.RightHandItemSlot.Itemstack.Item == null || !fromPlayer.Entity.RightHandItemSlot.Itemstack.Item.Code.ToString().StartsWith("machines:cable")) { return; }
                            fromPlayer.Entity.RightHandItemSlot.Itemstack.StackSize--;
                            if (fromPlayer.Entity.RightHandItemSlot.Itemstack.StackSize == 0)
                            {
                                fromPlayer.Entity.RightHandItemSlot.Itemstack = null;
                            }
                            fromPlayer.Entity.RightHandItemSlot.MarkDirty();
                        }
                    }
                }
            }
        }
        protected string textred = "<font color=\"#ff4444\">";
        protected string textyellow = "<font color=\"#ffff33\">";
        protected string textgreen = "<font color=\"#66ff66\">";
        protected string boldon = "<strong>";
        protected string boldoff = "</strong>";
        protected string textoff = "</font>";
        public virtual string GetStatusUI()
        {
            string statustext = "Power Grid Status:<br>";
            
            
            if (!IsOn) { statustext += textyellow + "Device Off!" + "</font>"; }
            else
            {
                PowerNetwork pn = FlexNetworkManager.GetNetworkWithID(NetworkID) as PowerNetwork;
                if (pn == null) { statustext += textred + boldon + "NO POWER GRID!" + boldoff + textoff; }
                else 
                {
                    if (pn.NetworkStatus.consumed < pn.NetworkStatus.generated)
                    {
                        statustext += textgreen;
                    }
                    else if (pn.NetworkStatus.consumed < pn.NetworkStatus.generated + pn.NetworkStatus.stored)
                    {
                        statustext += textyellow;
                    }
                    else
                    {
                        statustext += textred;
                    }
                    statustext += networkstatus + textoff;
                }
            }
            statustext += "<br>";
            return statustext;
        }
        GUIBEElectric gui;
        public virtual void OpenStatusGUI()
        {
            ICoreClientAPI capi = Api as ICoreClientAPI;
            

            if (capi != null)
            {
                if (gui == null)
                {
                    gui = new GUIBEElectric("Device Status", Pos, capi);

                    gui.TryOpen();
                    gui.SetupDialog(this);

                }
                else
                {
                    gui.TryClose();
                    gui.TryOpen();
                    gui.SetupDialog(this);
                }
            }

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
            
            if (tree.HasAttribute("directlinks"))
            {
                
                byte[] data = tree.GetBytes("directlinks");
                if (data.Length > 0)
                {
                    directlinks = SerializerUtil.Deserialize<List<BlockPos>>(data);
                }
            }
            else
            {
                directlinks = new List<BlockPos>();
            }


        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("lastPower", lastPower);
            tree.SetInt("storedFlux", storedFlux);
            PowerNetwork pn = FlexNetworkManager.GetNetworkWithID(NetworkID) as PowerNetwork;
            if (pn == null) { networkstatus = "INVALID NETWORK"; }
            else { networkstatus = pn.NetworkStatus.nodes + " devices. Network using " + pn.NetworkStatus.consumed + ", generating " + pn.NetworkStatus.generated + ", and storing " + pn.NetworkStatus.stored+" temporal flux."; }
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
            byte[] directdata = SerializerUtil.Serialize<List<BlockPos>>(DirectLinks);
            tree.SetBytes("directlinks", directdata);
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
