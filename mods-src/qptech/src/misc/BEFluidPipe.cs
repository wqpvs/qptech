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

namespace qptech.src
{
    class BEFluidPipe : BlockEntityContainer, IFluidTank
    {
        public List<BlockFacing> disabledFaces;
        public List<BlockFacing> soakerFaces;
        public int soaker = 0;
        public int CapacityLitres { get { return capacitylitres; } set{ capacitylitres = value; } }
        int capacitylitres = 50;
        public string metal
        {
            get
            {
                string content = Block.LastCodePart();
                if (content == "copper" || content == "bronze" || content == "bluesteel" || content == "steel") return content;
                return null;
            }
        }


        public bool IsFull { get { return CurrentLevel == CapacityLitres; } }

        public int CurrentLevel => inventory[0].StackSize;

        public Item CurrentItem => inventory[0] == null || inventory[0].Itemstack == null ? null : inventory[0].Itemstack.Item;
        public BEFluidPipe()
        {
            inventory = new InventoryGeneric(1, null, null);
        }

        public BlockPos TankPos => Pos;

        internal InventoryGeneric inventory;
        public override InventoryBase Inventory => inventory;
        public override string InventoryClassName => "pipe";

        BlockFacing[] facechecker = new BlockFacing[] { BlockFacing.DOWN, BlockFacing.NORTH, BlockFacing.EAST, BlockFacing.SOUTH, BlockFacing.WEST };

        protected override void OnTick(float dt)
        {
            
            Equalize();
            Soaker();
            //temp code to set a random face as inactive
            /*if (Api is ICoreServerAPI&&(disabledFaces==null||disabledFaces.Count==0))
            {
                Random r = new Random(Pos.X+Pos.Z+Pos.Y);
                int randomindex = r.Next(0, 5);
                disabledFaces.Add(BlockFacing.ALLFACES[randomindex]);
                MarkDirty(true);
            }*/
        }
        protected void OnFastTick(float dt)
        {
            NeighbourCheck();
        }
        
        void NeighbourCheck()
        {
            FillBarrels();
            DrainBarrels();
        }
        
        void DrainBarrels()
        {
            if (Api is ICoreClientAPI) { return; }
            if (disabledFaces.Contains(BlockFacing.UP)) { return; }
            BlockPos p = Pos.UpCopy();
            BlockEntityBarrel barrel = Api.World.BlockAccessor.GetBlockEntity(p) as BlockEntityBarrel;
            if (barrel == null) { return; }
            if (barrel.Sealed) { return; }
            if (barrel.CanSeal) { return; }
            if (barrel.Inventory == null) { return; }
            if (barrel.Inventory[1] == null) { return; }
            if (barrel.Inventory[1].StackSize == 0) { return; }
            if (inventory[0].Inventory.Empty || inventory[0].Itemstack == null || inventory[0].StackSize == 0 || inventory[0].Itemstack.Item == null)
            {
                int useamt = Math.Min(this.CapacityLitres, barrel.Inventory[1].StackSize);
                ItemStack newstack = new ItemStack(barrel.Inventory[1].Itemstack.Item, useamt);
                inventory[0].Itemstack = newstack;
                barrel.Inventory[1].Itemstack.StackSize -= useamt;
                if (barrel.Inventory[1].Itemstack.StackSize <= 0) { barrel.Inventory[1].Itemstack = null; }
                this.MarkDirty(true);
                barrel.MarkDirty(true);
            }

        }

        void FillBarrels()
        {
            //check for other containers
            if (Api is ICoreClientAPI) { return; }
            if (inventory==null||inventory.Empty||inventory[0].Itemstack==null||inventory[0].StackSize<1) { return; }
            if (!disabledFaces.Contains(BlockFacing.DOWN) && !inventory.Empty)
            {
                BlockPos p = Pos.DownCopy();
                BlockEntityBarrel barrel = Api.World.BlockAccessor.GetBlockEntity(p) as BlockEntityBarrel;
                if (barrel == null) { return; }
                if (barrel.Sealed) { return; }
                if (barrel.CanSeal) { return; }
                if (barrel.Inventory == null) { return; }
                if (barrel.Inventory[1] == null) { return; }
                
                if (barrel.Inventory[1].StackSize >= barrel.CapacityLitres) { return; }
                int useliquid = 0;
                if ((barrel.Inventory[1].Itemstack == null || barrel.Inventory[1].Itemstack.Item == null))
                {
                    
                    inventory[0].Itemstack.StackSize-=1;
                    ItemStack newstack = new ItemStack(inventory[0].Itemstack.Item, 1);
                    barrel.Inventory[1].Itemstack = newstack;
                    this.MarkDirty(true);
                    barrel.MarkDirty(true);
                    return;
                }

                if (barrel.Inventory[1].Itemstack.Item != inventory[0].Itemstack.Item) { return; }



                //if there is matching inventory than just change around amounts
                useliquid = 1;
                
                inventory[0].Itemstack.StackSize -= useliquid;
                barrel.Inventory[1].Itemstack.StackSize += useliquid;
               
                this.MarkDirty(true);
                barrel.MarkDirty(true);

            }
        }
        public void OnNeighborChange()
        {
            MarkDirty(true);
        }

        public void Soaker()
        {
            if (soaker == 0 || soakerFaces == null || soakerFaces.Count == 0) { return; }
            if (inventory.Empty||inventory[0].StackSize<soaker) { return; }
            if (inventory[0].Itemstack.Collectible.Code.ToString().Contains("water"))
            {
                int wateredblocks = 0; ;

                for (int xc = -1; xc < 2; xc++)
                {
                    for (int zc= -1; zc<2; zc++)
                    {
                        BlockPos p = Pos.Copy();
                        p.X += xc;
                        p.Z += zc;
                        p.Y += 1;
                        BlockEntityFarmland beup = Api.World.BlockAccessor.GetBlockEntity(p) as BlockEntityFarmland;
                        if (beup == null) { continue; }
                        if (beup.MoistureLevel > 0.75f) { continue; }
                        wateredblocks++;
                        beup.WaterFarmland(1, false);
                    }
                }
                if (wateredblocks == 0) { return; }
                inventory[0].Itemstack.StackSize -= soaker;
                if (inventory[0].Itemstack.StackSize <= 0) { inventory[0].Itemstack = null; }
                MarkDirty(true); 
               

            }
            
        }
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            soakerFaces = new List<BlockFacing>();
            if (Block.Attributes != null)
            {
                soaker= Block.Attributes["soaker"].AsInt(soaker);
                string[] soakerfacename;
                soakerfacename = Block.Attributes["soakerFaces"].AsArray<string>();
                capacitylitres = Block.Attributes["capacitylitres"].AsInt(capacitylitres);
                if (soakerfacename != null && soakerfacename.Length > 0)
                {
                    foreach (string s in soakerfacename)
                    {
                        BlockFacing bf = BlockFacing.FromCode(s);
                        if (bf == null) { continue; }
                        soakerFaces.Add(bf);
                        if (disabledFaces != null && !disabledFaces.Contains(bf)){ disabledFaces.Add(bf); }
                    }
                }
                else
                {
                    soaker = 0;
                }
            }
            RegisterGameTickListener(OnFastTick, 100);
        }   
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
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
                BEEGenerator g = ent as BEEGenerator;
                BEWaterTower w = ent as BEWaterTower;
                if (t == null && g == null && w == null && !isdisabled) { continue; }

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

            return base.OnTesselation(mesher, tessThreadTesselator);
        }
        public virtual void Equalize()
        {
            if (inventory.Empty) { return; }

            //Check for tanks below and beside and fill appropriately
            foreach (BlockFacing bf in facechecker) //used facechecker to make sure down is processed first
            {

                if (disabledFaces.Contains(bf)) { continue; }
                if (inventory.Empty) { break; }
                BlockEntityContainer outputContainer = Api.World.BlockAccessor.GetBlockEntity(Pos.Copy().Offset(bf)) as BlockEntityContainer;
                if (outputContainer == null) { continue; }              //is it a container?
                IFluidTank bt = outputContainer as IFluidTank;
                if (bt == null) { continue; }                           //is it a fellow tank person?
                if (outputContainer.Inventory == null) { continue; }    //a null inventory is weird, so skip it
                int outputQuantity = inventory[0].StackSize;            //default to dumping entire stack
                if (bf != BlockFacing.DOWN && bt.CurrentLevel > CurrentLevel) { continue; }    //beside us and already has more liquid
                if (bt.IsFull) { continue; }                         //its already full
                if (bf != BlockFacing.DOWN)
                {
                    int targetQuantity = (CurrentLevel + bt.CurrentLevel) / 2;
                    outputQuantity = CurrentLevel - targetQuantity;
                }
                int usedQuantity = bt.ReceiveFluidOffer(inventory[0].Itemstack.Item, outputQuantity, Pos);
                if (usedQuantity > 0)
                {
                    inventory[0].Itemstack.StackSize -= usedQuantity;
                    if (inventory[0].Itemstack.StackSize <= 0)
                    {
                        inventory[0].Itemstack = null;
                    }
                    MarkDirty(true);
                }
            }
        }

        public int ReceiveFluidOffer(Item offeredItem, int offeredAmount, BlockPos offeredFromPos)
        {
            if (disabledFaces != null&&disabledFaces.Count>0) {
                foreach (BlockFacing bf in disabledFaces)
                {
                    if (Pos.Copy().Offset(bf) == offeredFromPos)
                    {
                        return 0;
                    }
                }
            }
            if (inventory[0].Itemstack != null && inventory[0].Itemstack.Item != null && offeredItem != inventory[0].Itemstack.Item) { return 0; }
            int useamount = offeredAmount;
            useamount = Math.Min(CapacityLitres - CurrentLevel, useamount);
            if (useamount <= 0) { useamount = 0; }
            else if (inventory[0].Itemstack == null || inventory[0].Itemstack.Item == null)
            {
                ItemStack newstack = new ItemStack(offeredItem, useamount);
                inventory[0].Itemstack = newstack;
                MarkDirty(true);
            }
            else
            {
                inventory[0].Itemstack.StackSize += useamount;
                MarkDirty(true);
            }

            offeredAmount -= useamount;
            ///TODO Here we could push overflow?
            return useamount;
        }
        public int TryTakeFluid(int requestedamount, BlockPos offerFromPos)
        {
            int giveamount = 0;

            giveamount = Math.Min(requestedamount, CurrentLevel);
            inventory[0].Itemstack.StackSize -= giveamount;
            if (inventory[0].Itemstack.StackSize == 0) { inventory[0].Itemstack = null; }
            MarkDirty(true);
            return giveamount;
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            ItemSlot slot = inventory[0];

            if (slot.Empty)
            {
                dsc.AppendLine(Lang.Get("Empty"));
            }
            else
            {
                dsc.AppendLine(Lang.Get("Contents: {0}x{1}", slot.Itemstack.StackSize, slot.Itemstack.GetName()));
            }
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
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);

            var asString = tree.GetString("disabledfaces");
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
            if (soaker != 0)
            {
                if (soakerFaces!=null && soakerFaces.Count > 0)
                {
                    foreach (BlockFacing bf in soakerFaces)
                    {
                        if (!disabledFaces.Contains(bf)) { disabledFaces.Add(bf); }
                    }
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
                if (soaker != 0 && soakerFaces != null && soakerFaces.Count > 0)
                {
                    if (soakerFaces.Contains(subface)) { return true; }
                }
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
