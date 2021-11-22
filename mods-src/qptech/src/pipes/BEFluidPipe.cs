using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.API.Server;
using Newtonsoft.Json;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using qptech.src.networks;
namespace qptech.src
{
    class BEFluidPipe : BlockEntity, IFluidNetworkMember
    {
        public List<BlockFacing> disabledFaces;
        
        public bool filler=false;
        public bool drainer=false;
        bool locked = false;
        Guid networkID;
        public Guid NetworkID => networkID;
        public string ProductID => "FLUID";
        protected List<BlockEntity> inputNodes;
        protected List<BlockEntity> outputNodes;
        public string fluid;
        protected BlockFacing[] checkfaces;
        public List<BlockEntity> InputNodes() { if (inputNodes == null) { inputNodes = new List<BlockEntity>(); } return inputNodes; }
        public List<BlockEntity> OutputNodes() { if (outputNodes == null) { outputNodes = new List<BlockEntity>(); } return outputNodes; }
        public void NetworkRemove() {
            networkID = Guid.Empty;
            MarkDirty();
        }

        public virtual void NetworkJoin(Guid newnetwork)
        {
            if (newnetwork == Guid.Empty) { return; }
            //if (newnetwork == NetworkID) { return; }
            FlexNetworkManager.LeaveNetwork(NetworkID, this);
            //lastPower = 0;
            bool ok = FlexNetworkManager.JoinNetworkWithID(newnetwork, this as IFlexNetworkMember);
            if (ok)
            {
                networkID = newnetwork;
            }
            MarkDirty();
        }
        public virtual void OnPulse(string channel)
        {
            if (channel == "LOCK")
            {
                locked = true;
            }
            if (channel == "UNLOCK")
            {
                locked = false;
            }

        }
        public void SetFluid(string newfluid)
        {
            fluid = newfluid;
            MarkDirty();
        }
        int fluidrate =1;
        public int FluidRate => fluidrate;
        
        public int GetHeight()
        {
            return Pos.Y;
        }
        public int SetFluidLevel(int amt,string influid)
        {
            return 0;

        }


        public virtual Guid GetNetworkID(BlockPos requestedby, string fortype)
        {
            
            foreach (BlockFacing bf in BlockFacing.ALLFACES)
            {
                
                BlockPos checkpos = Pos.AddCopy(bf);
                if (requestedby == checkpos) {
                    if (disabledFaces!=null&&disabledFaces.Contains(bf)) { return Guid.Empty; }
                    return NetworkID;
                }
            }
            return Guid.Empty;
            
        }
        public string metal
        {
            get
            {
                string content = Block.LastCodePart();
                if (content == "copper" || content == "bronze" || content == "bluesteel" || content == "steel") return content;
                return null;
            }
        }
        
        public BlockPos TankPos => Pos;

        BlockFacing[] facechecker = new BlockFacing[] { BlockFacing.DOWN, BlockFacing.NORTH, BlockFacing.EAST, BlockFacing.SOUTH, BlockFacing.WEST };

        bool firsttick = false;
        protected void OnFastTick(float dt)
        {
            if (Api is ICoreServerAPI)
            {
                Guid og = networkID;
                DoConnections();
                if (NetworkID == Guid.Empty)
                {
                    networkID = FlexNetworkManager.RequestNewNetwork(ProductID);
                    
                }
                FlexNetworkManager.JoinNetworkWithID(networkID, this);
                if (networkID != og||!firsttick) { MarkDirty(true);firsttick = true; }

            }
            HandleFluidNetwork();
        }
        
        
        void AttachmentCheck()
        {
            BlockPos p = Pos.DownCopy();
            BlockEntityBarrel barrel = Api.World.BlockAccessor.GetBlockEntity(p) as BlockEntityBarrel;
            bool dothedirty = false;
            bool oldfiller = filler;
            if (barrel != null) { filler = true; }
            else { filler = false; }
            if (filler != oldfiller) { dothedirty=true; }
            p = Pos.UpCopy();
            barrel = Api.World.BlockAccessor.GetBlockEntity(p) as BlockEntityBarrel;
            bool olddrainer = drainer;
            if (barrel != null) { drainer = true; }
            else { drainer = false; }
            if (olddrainer != drainer) { dothedirty = true; }
            if (dothedirty) { MarkDirty(true); }
        }
        
        public void OnNeighborChange()
        {
            MarkDirty(true);
        }

        
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            checkfaces = BlockFacing.ALLFACES;
            
            
            if (NetworkID != Guid.Empty && (api is ICoreServerAPI))
            {
                FlexNetworkManager.RecreateNetwork(NetworkID, ProductID);
                MarkDirty(true);
            }
            RegisterGameTickListener(OnFastTick, 100);
        }   
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            if (this is BEFluidPump) { return base.OnTesselation(mesher, tessThreadTesselator); }
            ICoreClientAPI capi = Api as ICoreClientAPI;
            if (capi == null) { return base.OnTesselation(mesher, tessThreadTesselator); }
            Block pipesegment = Api.World.GetBlock(new AssetLocation("machines:pipe-segment" + "-" + (metal)));
            MeshData mesh;
            Cuboidf[] collboxes = Block.GetCollisionBoxes(Api.World.BlockAccessor, Pos);
            //Note the pipesegment by default is north facing
            foreach (BlockFacing bf in BlockFacing.ALLFACES)
            {
                bool isdisabled = false;
                if (disabledFaces != null && disabledFaces.Contains(bf))
                {
                    pipesegment = Api.World.GetBlock(new AssetLocation("machines:pipe-closed" + "-" + (metal)));
                    isdisabled = true;
                }
                else
                {
                    pipesegment = Api.World.GetBlock(new AssetLocation("machines:pipe-segment" + "-" + (metal)));
                }
                BlockEntity ent = Api.World.BlockAccessor.GetBlockEntity(Pos.Copy().Offset(bf));
                if (ent == null && !isdisabled) { continue; }
                IFluidTank t = ent as IFluidTank;
                IFlexNetworkMember ifn = ent as IFluidNetworkMember;
                IFluidNetworkUser ifnu = ent as IFluidNetworkUser;
                BEEGenerator g = ent as BEEGenerator;
                BEWaterTower w = ent as BEWaterTower;
                if (ifnu==null && ifn==null && t == null && g == null && w == null && !isdisabled) { continue; }
                if (ifn!=null && ifn.NetworkID != NetworkID) { continue; }
                capi.Tesselator.TesselateBlock(pipesegment, out mesh);
                if (bf == BlockFacing.NORTH)
                {
                    mesher.AddMeshData(mesh);

                    //do nothing, the block is setup how we want it
                }
                else if (bf == BlockFacing.EAST)
                {
                    mesher.AddMeshData(mesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, GameMath.DEG2RAD * 270, 0));
     
                }
                else if (bf == BlockFacing.SOUTH)
                {
                    mesher.AddMeshData(mesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, GameMath.DEG2RAD * 180, 0));
                }
                else if (bf == BlockFacing.WEST)
                {
                    mesher.AddMeshData(mesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, GameMath.DEG2RAD * 90, 0));
                }
                else if (bf == BlockFacing.UP)
                {

                    mesher.AddMeshData(mesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), GameMath.DEG2RAD * 90, 0, 0));
  
                }
                else if (bf == BlockFacing.DOWN)
                {
                    mesher.AddMeshData(mesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), -GameMath.DEG2RAD * 90, 0, 0));
        
                }
            }
            if (disabledFaces!=null&&filler&&!disabledFaces.Contains(BlockFacing.DOWN))
            {
                
                Block sprinkler = Api.World.GetBlock(new AssetLocation("machines:pipe-sprinkler"));
                capi.Tesselator.TesselateBlock(sprinkler, out mesh);
                mesher.AddMeshData(mesh.Clone());
            }
            if (disabledFaces!=null&&drainer && !disabledFaces.Contains(BlockFacing.UP))
            {

                Block drainer = Api.World.GetBlock(new AssetLocation("machines:pipe-drainer"));
                capi.Tesselator.TesselateBlock(drainer, out mesh);
                mesher.AddMeshData(mesh.Clone());
            }

            return base.OnTesselation(mesher, tessThreadTesselator);
        }
        public virtual void DoConnections()
        {

            inputNodes = new List<BlockEntity>();
            outputNodes = new List<BlockEntity>();
            //Check for tanks below and beside and fill appropriately
            foreach (BlockFacing bf in checkfaces) //used facechecker to make sure down is processed first
            {

                if (disabledFaces.Contains(bf)) { continue; }
                IFluidNetworkMember fnm = Api.World.BlockAccessor.GetBlockEntity(Pos.Copy().Offset(bf)) as IFluidNetworkMember;
                if (fnm != null)
                {

                    continue;
                }

                ///HERE IS WHERE WE'LL PUT CODE TO FILL IN OUTPUT NODE INFO
                ///
                BlockEntity finde = Api.World.BlockAccessor.GetBlockEntity(Pos.Copy().Offset(bf)) as BlockEntity;
                
                if (finde == null) { continue; }              //is it a container?
                IFluidTank bt = finde as IFluidTank;
                BlockEntityBarrel beb = finde as BlockEntityBarrel;
                IFluidNetworkUser fnu = finde as IFluidNetworkUser;
                if (bt==null && beb == null&&fnu==null) { continue; }
                if (beb!=null && bf != BlockFacing.DOWN && bf != BlockFacing.UP) { continue; } //barrels only connect up/down
                if (beb != null)
                {
                    bool ogdrainer = drainer;drainer = false;
                    bool ogfiller = filler;filler = false;
                    if (bf == BlockFacing.DOWN) { filler = true;  }
                    else if (bf == BlockFacing.UP) { drainer = true;  }
                    if (ogdrainer != drainer || ogfiller != filler) { MarkDirty(true); }                }
                if (fnu != null && fnu.IsOnlyDestination()) { outputNodes.Add(finde); }
                else if (fnu!=null && fnu.IsOnlySource()) { inputNodes.Add(finde); }
                else if (bf == BlockFacing.DOWN)
                {
                    outputNodes.Add(finde);
                }
                else 
                {
                    inputNodes.Add(finde);
                }

            }    
                
        }
        protected virtual void HandleFluidNetwork()
        {
            
            
            foreach (BlockFacing bf in checkfaces) //used facechecker to make sure down is processed first
            {

                if (disabledFaces!=null&&disabledFaces.Contains(bf)) { continue; }
                IFluidNetworkMember fnm = Api.World.BlockAccessor.GetBlockEntity(Pos.Copy().Offset(bf)) as IFluidNetworkMember;
                if (fnm == null)
                {

                    continue;
                }
                Guid fnid = fnm.GetNetworkID(Pos, ProductID);
                IFlexNetwork othernet = FlexNetworkManager.GetNetworkWithID(fnid);
                if (othernet == null) { continue; }
                
                IFlexNetwork mynet = FlexNetworkManager.GetNetworkWithID(NetworkID);
                if (mynet == null) { NetworkJoin(fnid); break; }
                if (mynet.GetMembers().Count > othernet.GetMembers().Count) { continue; }
                if (fnid != Guid.Empty && (networkID == Guid.Empty || fnid != networkID))
                {
                    
                    NetworkJoin(fnid);
                    break;
                }
            }
            
        }
    
    
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            
            if (disabledFaces != null && disabledFaces.Count > 0)
            {
                foreach (BlockFacing bf in disabledFaces)
                {
                    dsc.AppendLine(bf.ToString() + " is disabled");
                }
            }
            if (looky != null)
            {
                //dsc.AppendLine(looky.HitPosition.ToString());
                dsc.AppendLine("Selecting "+GetSubFace(looky).ToString());
            }
            
                
            dsc.AppendLine("NetworkID " + networkID.ToString());
            if (locked) { dsc.AppendLine("Fluid Network Locked ("+fluid+")"); }
            else { dsc.AppendLine("Fluid Network Open (" + fluid + ")"); }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            if (disabledFaces == null) { disabledFaces = new List<BlockFacing>(); }
            List<string> dfstring = new List<string>();
            foreach (BlockFacing bf in disabledFaces)
            {
                dfstring.Add(bf.ToString());
            }
            var asString = JsonConvert.SerializeObject(dfstring);
            tree.SetString("disabledfaces", asString);
            if (networkID != Guid.Empty)
            {
                tree.SetString("networkID", networkID.ToString());
            }
            else
            {
                tree.SetString("networkID", "");
            }
            tree.SetBool("filler", filler);
            tree.SetBool("drainer", drainer);
            tree.SetBool("locked", locked);
            tree.SetString("fluid", fluid);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            
            string gid = "";
            gid = tree.GetString("networkID");
            
            if (gid != "" && gid!=null)
            {
                networkID = Guid.Parse(gid);
            }
            else { networkID = Guid.Empty; }
            var asString = tree.GetString("disabledfaces");
            filler = tree.GetBool("filler");
            drainer = tree.GetBool("drainer");
            locked = tree.GetBool("locked");
            fluid = tree.GetString("fluid");
            List<string> dfstring = new List<string>();
            if (asString != "")
            {
                try
                {
                    dfstring = JsonConvert.DeserializeObject<List<string>>(asString);
                    disabledFaces = new List<BlockFacing>();
                    foreach (string s in dfstring)
                    {
                        disabledFaces.Add(BlockFacing.FromCode(s));
                    }
                }
                catch
                {
                    disabledFaces = new List<BlockFacing>();
                }
            }
            
            
        }

        
        public bool OnInteract(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (byPlayer.InventoryManager.ActiveHotbarSlot.Empty) { return false; }
            if (byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack == null) { return false; }
            if (byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Item == null) { return false; }
            if (!byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Item.Code.ToString().Contains("pipewrench")) { return false; }
            
            
            if (Api is ICoreServerAPI)
            {
                BlockFacing subface = GetSubFace(blockSel);
                
                if (disabledFaces == null) { disabledFaces = new List<BlockFacing>(); }
                
                if (disabledFaces.Contains(subface)) { disabledFaces.Remove(subface); }
                else { disabledFaces.Add(subface); }
                MarkDirty(true);
            }
            //(Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, (int)enPacketIDs.Wrench, data);
            else
            {
                Api.World.PlaySoundAt(new AssetLocation("sounds/valve"), Pos.X, Pos.Y, Pos.Z, byPlayer, false, 8, 1);
            }
            return true;
        }
        BlockSelection looky;
        public void OnBeingLookedAt(IPlayer byPlayer, BlockSelection blockSel, bool firstTick)
        {
            looky = blockSel;
        }
        public override void OnBlockBroken()
        {
            base.OnBlockBroken();
            if (Api is ICoreServerAPI)
            {
                FlexNetworkManager.DeleteNetwork(NetworkID);
            }
        }
        const float selectionzone = 0.4f;
       public BlockFacing GetSubFace(BlockSelection blockSelection)
        {
            if (BlockFacing.HORIZONTALS.Contains(blockSelection.Face))
            {
                if (blockSelection.HitPosition.Y < selectionzone) { return BlockFacing.DOWN; }
                else if (blockSelection.HitPosition.Y > 1 - selectionzone) { return BlockFacing.UP; }
                else if (blockSelection.Face == BlockFacing.EAST || blockSelection.Face == BlockFacing.WEST)
                {
                    if (blockSelection.HitPosition.Z < selectionzone) { return BlockFacing.NORTH; }
                    else if (blockSelection.HitPosition.Z > 1 - selectionzone) { return BlockFacing.SOUTH; }
                }
                else if (blockSelection.Face== BlockFacing.NORTH || blockSelection.Face == BlockFacing.SOUTH)
                {
                    if (blockSelection.HitPosition.X < selectionzone) { return BlockFacing.WEST; }
                    else if (blockSelection.HitPosition.X > 1 - selectionzone) { return BlockFacing.EAST; }
                }
            }
            else
            {
                if (blockSelection.HitPosition.Z < selectionzone) { return BlockFacing.NORTH; }
                else if (blockSelection.HitPosition.Z > 1 - selectionzone) { return BlockFacing.SOUTH; }
                else if (blockSelection.HitPosition.X < selectionzone) { return BlockFacing.WEST; }
                else if (blockSelection.HitPosition.X > 1 - selectionzone) { return BlockFacing.EAST; }
            }
            
            return blockSelection.Face;
        }
    }
}
