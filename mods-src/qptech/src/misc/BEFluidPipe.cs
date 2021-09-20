﻿using System;
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
using Vintagestory.API.Client;
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
        public int CapacityLitres { get; set; } = 25;

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
            NeighbourCheck();
            Equalize();

            //temp code to set a random face as inactive
            /*if (Api is ICoreServerAPI&&(disabledFaces==null||disabledFaces.Count==0))
            {
                Random r = new Random(Pos.X+Pos.Z+Pos.Y);
                int randomindex = r.Next(0, 5);
                disabledFaces.Add(BlockFacing.ALLFACES[randomindex]);
                MarkDirty(true);
            }*/
        }

        void NeighbourCheck()
        {

        }
        public void OnNeighborChange()
        {
            MarkDirty(true);
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            ICoreClientAPI capi = Api as ICoreClientAPI;
            if (capi == null) { return base.OnTesselation(mesher, tessThreadTesselator); }
            Block pipesegment = Api.World.GetBlock(new AssetLocation("machines:dummy-pipesegment"));
            MeshData mesh;
            //Note the pipesegment by default is north facing
            foreach (BlockFacing bf in BlockFacing.ALLFACES)
            {
                bool isdisabled = false;
                if (disabledFaces != null && disabledFaces.Contains(bf))
                {
                    pipesegment = Api.World.GetBlock(new AssetLocation("machines:dummy-pipeblock"));
                    isdisabled = true;
                }
                else
                {
                    pipesegment = Api.World.GetBlock(new AssetLocation("machines:dummy-pipesegment"));
                }
                BlockEntity ent = Api.World.BlockAccessor.GetBlockEntity(Pos.Copy().Offset(bf));
                if (ent == null&&!isdisabled) { continue; }
                IFluidTank t = ent as IFluidTank;
                BEEGenerator g = ent as BEEGenerator;
                BEWaterTower w = ent as BEWaterTower;
                if (t == null&&g==null&&w==null&&!isdisabled) { continue; }
                
                capi.Tesselator.TesselateBlock(pipesegment, out mesh);
                if (bf == BlockFacing.NORTH)
                {
                    mesher.AddMeshData(mesh);
                    //do nothing, the block is setup how we want it
                }
                else if (bf == BlockFacing.EAST)
                {
                    mesher.AddMeshData(mesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0,GameMath.DEG2RAD*270, 0));
                }
                else if (bf == BlockFacing.SOUTH)
                {
                    mesher.AddMeshData(mesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, GameMath.DEG2RAD *180, 0));
                }
                else if (bf == BlockFacing.WEST)
                {
                    mesher.AddMeshData(mesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, GameMath.DEG2RAD * 90, 0));
                }
                else if (bf == BlockFacing.UP)
                {
                    mesher.AddMeshData(mesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), GameMath.DEG2RAD * 90,0, 0));
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
        }
        private byte[] ObjectToByteArray(Object obj)
        {
            if (obj == null)
                return null;

            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, obj);

            return ms.ToArray();
        }

        // Convert a byte array to an Object
        private Object ByteArrayToObject(byte[] arrBytes)
        {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            Object obj = (Object)binForm.Deserialize(memStream);

            return obj;
        }
        
        public bool OnInteract(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (byPlayer.InventoryManager.ActiveHotbarSlot.Empty) { return false; }
            if (byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack == null) { return false; }
            if (byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Item == null) { return false; }
            if (!byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Item.Code.ToString().Contains("pipewrench")) { return false; }
            
            string faceclicked = blockSel.Face.ToString();
            byte[] data = ObjectToByteArray(faceclicked);
            if (Api is ICoreServerAPI)
            {
                if (disabledFaces == null) { disabledFaces = new List<BlockFacing>(); }
                if (disabledFaces.Contains(blockSel.Face)) { disabledFaces.Remove(blockSel.Face); }
                else { disabledFaces.Add(blockSel.Face); }
                MarkDirty(true);
            }
            //(Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, (int)enPacketIDs.Wrench, data);
            else
            {
                Api.World.PlaySoundAt(new AssetLocation("sounds/valve"), Pos.X, Pos.Y, Pos.Z, byPlayer, false, 8, 1);
            }
            return true;
        }

       
    }
}
