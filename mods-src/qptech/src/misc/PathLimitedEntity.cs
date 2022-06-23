﻿using System;
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
using Vintagestory.API.MathTools;
using qptech.src.extensions;
using System.Text.RegularExpressions;

namespace qptech.src.misc
{
    class PathLimitedEntity:Entity
    {
        Vec3d pathstart;
        Vec3d pathend;
        double pathdir = 1;
        double pathprogress=0;
        double pathspeed = 0.1f;
        bool moving = false;
        CollisionTester collTester = new CollisionTester();
        EntityPartitioning ep;
        BlockFacing heading = BlockFacing.NORTH;
        Vec3d pathpos => new Vec3d(GameMath.Lerp(pathstart.X, pathend.X, pathprogress)+0.5, GameMath.Lerp(pathstart.Y, pathend.Y, pathprogress), GameMath.Lerp(pathstart.Z, pathend.Z, pathprogress)+0.5);
        string pathcodecontains = "rails";
        string dropitem = "machines:creature-testentity";
        ICoreServerAPI sapi;
        InventoryGeneric inventory;
        public virtual InventoryGeneric Inventory => inventory;
        int inventorysize = 1;
        bool startpathset = false;
        
        public override bool IsInteractable
        {
            get { return true; }
        }
        public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
        {
            base.Initialize(properties, api, InChunkIndex3d);
            if (Attributes != null)
            {
                pathcodecontains = Attributes.GetString("pathcodecontains", pathcodecontains);
            }
            if (api is ICoreServerAPI)
            {
                sapi = api as ICoreServerAPI;
                inventory = new InventoryGeneric(inventorysize, "cart", "cart", Api);
                TryLoadInventory();
                TryStartPath();
                ep = api.ModLoader.GetModSystem<EntityPartitioning>();
                
            }
        }
        /*public override void OnEntitySpawn()
        {
            base.OnEntitySpawn();
            if (Api is ICoreClientAPI) { return; }
            sapi = Api as ICoreServerAPI;
            inventory = new InventoryGeneric(inventorysize, "cart", "cart", Api);
            TryLoadInventory();
            
            ep = Api.ModLoader.GetModSystem<EntityPartitioning>();
            Vec3d begin = ServerPos.XYZ;
            begin.X = Math.Floor(begin.X);
            begin.Y = Math.Floor(begin.Y);
            begin.Z = Math.Floor(begin.Z);
            pathstart = begin;
            FindPath();
        }*/
        
        public virtual void Stop()
        {
            moving = false;
            
        }
        public virtual void Start()
        {
            moving = true;

            FindPath();
        }
        public virtual void Start(Vec3d hitPostion)
        {
            if (hitPostion.X == 0.5 && heading==BlockFacing.EAST) { heading = BlockFacing.WEST; }
            else if (hitPostion.X==-0.5 && heading == BlockFacing.WEST) { heading = BlockFacing.EAST; }
            else if (hitPostion.Z==0.5 && heading == BlockFacing.SOUTH) { heading = BlockFacing.NORTH; }
            else if (hitPostion.Z==-0.5 && heading == BlockFacing.NORTH) { heading = BlockFacing.SOUTH; }
            
            Start();
        }

        string GetEntityStorageKey()
        {
            return "cartinventory" + EntityId.ToString();
        }

        void TryLoadInventory()
        {
            if (sapi == null) { return; }   

            try
            {

                List<byte> datalist = sapi.WorldManager.SaveGame.GetData<List<byte>>(GetEntityStorageKey());
                byte[] data;
                ITreeAttribute loadtree = null;
                if (datalist != null)
                {
                    data = datalist.ToArray();
                    loadtree = TreeAttribute.CreateFromBytes(data);
                }

                if (loadtree != null)
                {
                    ItemSlot[] slots = Inventory.SlotsFromTreeAttributes(loadtree);
                    int c = 0;
                    foreach (ItemSlot slot in slots)
                    {
                        if (!slot.Empty)
                        {
                            Inventory[c] = slot;
                        }
                        c++;
                        if (c == Inventory.Count) { break; }
                    }
                    Inventory.ResolveBlocksOrItems();

                }
                UpdateInventoryDisplay();
            }
            catch
            {
                int oops = 1;
            }
            
            
        }

        void TrySaveInventory()
        {
            if (Api is ICoreClientAPI) { return; }
            TreeAttribute newtree = new TreeAttribute();

            Inventory.SlotsToTreeAttributes(Inventory.ToArray<ItemSlot>(), newtree);

            //newtree will have correctly have the inventory at this point
            byte[] data = newtree.ToBytes();
            List<byte> datalist = data.ToList<byte>();

            //SAVE TO FILE ApiExtensions.SaveDataFile<List<byte>>(Api, GetChestFilename(player), datalist);
            sapi.WorldManager.SaveGame.StoreData<List<byte>>(GetEntityStorageKey(), datalist);
            UpdateInventoryDisplay();


        }
        public virtual void UpdateInventoryDisplay()
        {
            if (Inventory != null)
            {
                if (Inventory.Empty)
                {
                    holding = "Empty";
                    WatchedAttributes.SetString("holding", holding);
                    WatchedAttributes.MarkPathDirty("holding");
                }
                else
                {
                    foreach (ItemSlot slot in Inventory)
                    {
                        if (slot == null || slot.Empty) { continue; }
                        holding = slot.Itemstack.Collectible.GetHeldItemName(slot.Itemstack);
                        WatchedAttributes.SetString("holding", holding);
                        WatchedAttributes.MarkPathDirty("holding");
                    }
                }
            }
        }
        long msinteract;
        public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode)
        {
            base.OnInteract(byEntity, itemslot, hitPosition, mode);
            if (Api.World.ElapsedMilliseconds + 200 < msinteract) { return; }
            msinteract = Api.World.ElapsedMilliseconds+200;
            if (Api is ICoreServerAPI) {
                if (mode == EnumInteractMode.Interact&&Inventory!=null)
                {
                    //allow right click with item stack to load cart
                    if (itemslot != null && !itemslot.Empty)
                    {
                        foreach (ItemSlot myslot in Inventory)
                        {
                            if (myslot.CanHold(itemslot))
                            {
                                itemslot.TryPutInto(Api.World,myslot,itemslot.StackSize);
                                itemslot.MarkDirty();
                                myslot.MarkDirty();
                                TrySaveInventory();
                                
                                holding = myslot.Itemstack.Collectible.GetHeldItemName(myslot.Itemstack);
                                WatchedAttributes.MarkPathDirty("holding");
                                return;
                            }
                        }
                    }
                    else if (itemslot != null && itemslot.Empty)
                    {
                        foreach (ItemSlot myslot in Inventory)
                        {
                            if (itemslot.CanHold(myslot))
                            {
                                myslot.TryPutInto(Api.World, itemslot,myslot.StackSize);
                                itemslot.MarkDirty();
                                myslot.MarkDirty();
                                if (myslot.Empty)
                                {
                                    holding = "Empty";
                                    WatchedAttributes.MarkPathDirty("holding");
                                }
                                TrySaveInventory();
                                return;
                            }
                        }
                    }
                }
                else
                {
                    if (itemslot != null && itemslot.Itemstack != null && itemslot.Itemstack.Item != null && itemslot.Itemstack.Collectible.Code.ToString().Contains("wrench"))
                    {
                        Die(EnumDespawnReason.PickedUp);
                    }
                    if (moving) { Stop(); }
                    else { Start(hitPosition); }
                }
            }
        }
        public virtual void TryStartPath()
        {
            Vec3d begin = ServerPos.XYZ;
            begin.X = Math.Floor(begin.X);
            begin.Y = Math.Floor(begin.Y);
            begin.Z = Math.Floor(begin.Z);
            pathstart = begin;
            FindPath();
        }
        public override void OnGameTick(float dt)
        {
            base.OnGameTick(dt);
            if (Api is ICoreServerAPI)
            {
                if (!startpathset)
                {
                    TryStartPath();
                }
                Move();
            }
        }

        void Move()
        {
            if (!moving||pathend==null) { return; }
            pathprogress += pathdir * pathspeed;
            
            
            if (pathprogress >=1) {
                pathprogress = 1;pathstart = pathend;
                
                FindPath();
            }
            ServerPos.SetPos(pathpos);

        }

        protected virtual void FindPath()
        {
            if (!HandleInventory()) { return; }
            moving = false;
            pathprogress = 0;
            BlockPos p = pathstart.AsBlockPos;
            Block b = Api.World.BlockAccessor.GetBlock(p);
            if (!b.FirstCodePart().Contains(pathcodecontains)) { moving = false;return; }
            Block n = Api.World.BlockAccessor.GetBlock(p.Copy().Offset(BlockFacing.NORTH));
            Block s = Api.World.BlockAccessor.GetBlock(p.Copy().Offset(BlockFacing.SOUTH));
            Block e = Api.World.BlockAccessor.GetBlock(p.Copy().Offset(BlockFacing.EAST));
            Block w = Api.World.BlockAccessor.GetBlock(p.Copy().Offset(BlockFacing.WEST));
            bool nOK = n.FirstCodePart().Contains(pathcodecontains);
            bool eOK = e.FirstCodePart().Contains(pathcodecontains);
            bool sOK = s.FirstCodePart().Contains(pathcodecontains);
            bool wOK = w.FirstCodePart().Contains(pathcodecontains);
            BlockFacing newheading = heading;
            //pick new destination based on block we are currently in, where we were headed, and if the possible destination blocks were rails
            if (b.LastCodePart().Contains("flat_ns"))
            {
                if (nOK && !sOK) { newheading = BlockFacing.NORTH; moving = true; }
                else if (!nOK && sOK) { newheading = BlockFacing.SOUTH;moving = true; }
                else if (nOK && sOK)
                {
                    //this is a ns track and we were already going ns continue
                    if (heading == BlockFacing.NORTH ||heading == BlockFacing.SOUTH) { newheading = heading; moving = true; }
                    else if (heading == BlockFacing.EAST) { newheading = BlockFacing.SOUTH; moving = true; }//always turn right
                    else  { newheading = BlockFacing.NORTH; moving = true; }
                }
                else
                {
                    bool oops = true;
                }
            }
            else if (b.LastCodePart().Contains("flat_we"))
            {
                if (eOK && !wOK) { newheading = BlockFacing.EAST;moving = true; }
                else if (!eOK && wOK) { newheading = BlockFacing.WEST;moving = true; }
                else if (eOK && wOK) {
                    if (heading == BlockFacing.WEST || heading == BlockFacing.EAST)
                    {
                        newheading = heading;moving = true;
                    }
                    else if (heading == BlockFacing.NORTH) { newheading = BlockFacing.EAST; moving = true; }
                    else { newheading = BlockFacing.WEST; moving = true; }
                }
                else
                {
                    bool oops = true;
                }
            }
            else if (b.LastCodePart().Contains("curved_es"))
            {
                if (eOK && !sOK) { newheading = BlockFacing.EAST; moving = true; }
                else if (!eOK && sOK) { newheading = BlockFacing.SOUTH; moving = true; }
                else if (eOK && sOK)
                {
                    if (heading == BlockFacing.NORTH )
                    {
                        newheading = BlockFacing.EAST; moving = true;
                    }
                    else
                    {
                        newheading = BlockFacing.SOUTH;moving = true;
                    }
                }
                
            }
            else if (b.LastCodePart().Contains("curved_wn"))
            {
                if (wOK && !nOK) { newheading = BlockFacing.WEST; moving = true; }
                else if (!wOK && nOK) { newheading = BlockFacing.NORTH; moving = true; }
                else if (wOK && nOK)
                {
                    if (heading == BlockFacing.SOUTH)
                    {
                        newheading = BlockFacing.WEST; moving = true;
                    }
                    else
                    {
                        newheading = BlockFacing.NORTH; moving = true;
                    }
                }
            }
            else if (b.LastCodePart().Contains("curved_ne"))
            {
                if (eOK && !nOK) { newheading = BlockFacing.EAST; moving = true; }
                else if (!eOK && nOK) { newheading = BlockFacing.NORTH; moving = true; }
                else if (eOK && nOK)
                {
                    if (heading == BlockFacing.SOUTH)
                    {
                        newheading = BlockFacing.EAST; moving = true;
                    }
                    else
                    {
                        newheading = BlockFacing.NORTH; moving = true;
                    }
                }
            }
            else if (b.LastCodePart().Contains("curved_sw"))
            {
                if (sOK && !wOK) { newheading = BlockFacing.SOUTH; moving = true; }
                else if (!sOK && wOK) { newheading = BlockFacing.WEST; moving = true; }
                else if (sOK && wOK)
                {
                    if (heading == BlockFacing.NORTH)
                    {
                        newheading = BlockFacing.WEST; moving = true;
                    }
                    else
                    {
                        newheading = BlockFacing.SOUTH; moving = true;
                    }
                }
            }
            if (moving)
            {
                startpathset = true;
                pathprogress = 0;
                heading = newheading;
                
                
                pathend = pathstart + newheading.Normald;

                if (newheading == BlockFacing.SOUTH)
                {
                    ServerPos.SetYaw(180* 0.0174533f);
                }
                else if (newheading == BlockFacing.EAST)
                {
                    ServerPos.SetYaw(270 * 0.0174533f);
                }
                else if (newheading == BlockFacing.NORTH)
                {
                    ServerPos.SetYaw(0 * 0.0174533f);
                }
                else if (newheading == BlockFacing.WEST)
                {
                    ServerPos.SetYaw(90* 0.0174533f);
                }
                MarkMovementDirty();
            }
            else
            {
                bool oops = true;
            }
        }
/// <summary>
/// Handle Inventory - check for various spots to load & unload
/// </summary>
/// <returns>true=ok to move, false=wait</returns>
        protected virtual bool HandleInventory()
        {
            bool shouldmove= HandleUnloading()&&HandleLoading();
            if (!shouldmove) { TrySaveInventory(); }
            return shouldmove;
        }
        
        
        protected virtual bool HandleUnloading()
        {
            if (Inventory.Empty) { return true; }
            //Check below for hopper
            BlockPos p = ServerPos.AsBlockPos;
            p.Y -= 1;
            BlockEntity b = Api.World.BlockAccessor.GetBlockEntity(p);
            if (p == null)
            {
                return true;
            }
            BlockEntityGenericTypedContainer outcont = b as BlockEntityGenericTypedContainer;
            if (outcont != null&&outcont.Inventory!=null)
            {
                foreach (ItemSlot myslot in Inventory) {
                    if (myslot == null || myslot.Empty) { continue; }
                    foreach (ItemSlot slot in outcont.Inventory)
                    {
                        
                        if (!slot.CanHold(myslot)) { continue; }
                        
                        int moved =myslot.TryPutInto(Api.World, slot);
                        myslot.MarkDirty();
                        slot.MarkDirty();
                        if (moved > 0) {  return false; }
                        
                    }
                }
            }
            return true;
        }

        public virtual bool HandleLoading()
        {
            BlockPos p = ServerPos.AsBlockPos;
            p.Y +=2;
            BlockEntity b = Api.World.BlockAccessor.GetBlockEntity(p);
            if (b == null)
            {
                return true;
            }
            BlockEntityGenericTypedContainer incont = b as BlockEntityGenericTypedContainer;
            if (incont == null || incont.Inventory.Empty) { return true; }
            foreach (ItemSlot srcslot in incont.Inventory)
            {
                if (srcslot == null || srcslot.Empty) { continue; }
                foreach (ItemSlot destslot in Inventory)
                {
                    if (destslot == null) { continue; }
                    if (!destslot.CanHold(srcslot)) { continue; }
                    int moved = srcslot.TryPutInto(Api.World, destslot);
                    srcslot.MarkDirty();
                    destslot.MarkDirty();
                    if (moved > 0) {  return false; }
                }
            }
            return true;
        }


        public virtual string inventorykey => "cartinventory";


        public virtual void MarkMovementDirty()
        {
            WatchedAttributes.MarkPathDirty("moving");
            WatchedAttributes.MarkPathDirty("heading");
            WatchedAttributes.MarkPathDirty("startpathset");
            WatchedAttributes.MarkPathDirty("pathstart");
            WatchedAttributes.MarkPathDirty("pathend");
            WatchedAttributes.MarkPathDirty("pathprogress");
            WatchedAttributes.MarkPathDirty("pathdir");
            //WatchedAttributes.MarkPathDirty("pathendX");
            //WatchedAttributes.MarkPathDirty("pathendY");
            //WatchedAttributes.MarkPathDirty("pathendZ");
            //WatchedAttributes.MarkPathDirty("pathstartX");
            //WatchedAttributes.MarkPathDirty("pathstartY");
            //WatchedAttributes.MarkPathDirty("pathstartZ");
        }
        public override void ToBytes(BinaryWriter writer, bool forClient)
        {


            if (!forClient)
            {
                WatchedAttributes.SetBool("moving", moving);
                WatchedAttributes.SetString("heading", heading.ToString());
                WatchedAttributes.SetBool("startpathset", startpathset);
                WatchedAttributes.SetString("holding", holding);
                WatchedAttributes.SetDouble("pathdir", pathdir);
                WatchedAttributes.SetDouble("pathprogress", pathprogress);
                WatchedAttributes.SetDouble("pathendX", pathend.X);
                WatchedAttributes.SetDouble("pathendY", pathend.Y);
                WatchedAttributes.SetDouble("pathendZ", pathend.Z);
                WatchedAttributes.SetDouble("pathstartX", pathstart.X);
                WatchedAttributes.SetDouble("pathstartY", pathstart.Y);
                WatchedAttributes.SetDouble("pathstartZ", pathstart.Z);
            }
            base.ToBytes(writer, forClient);
        }
        string holding="Empty" ;
        public override void FromBytes(BinaryReader reader, bool fromServer)
        {
            base.FromBytes(reader, fromServer);
            moving = WatchedAttributes.GetBool("moving");
            string tryheading = WatchedAttributes.GetString("heading");
            holding = WatchedAttributes.GetString("holding");
            heading = BlockFacing.FromCode(tryheading);
            startpathset = WatchedAttributes.GetBool("startpathset");
            pathdir = WatchedAttributes.GetDouble("pathdir", 1);
            pathprogress = WatchedAttributes.GetDouble("pathprogress", 0);
            pathend = new Vec3d(
                WatchedAttributes.GetDouble("pathendX",0),
                WatchedAttributes.GetDouble("pathendY",0),
                WatchedAttributes.GetDouble("pathendZ",0)
                );
            pathstart = new Vec3d(
                WatchedAttributes.GetDouble("pathstartX", 0),
                WatchedAttributes.GetDouble("pathstartY", 0),
                WatchedAttributes.GetDouble("pathstartZ", 0)
                );
            if (pathstart.Y == 0 || pathend.Y == 0)
            {
                startpathset = false;

            }
            /*
             WatchedAttributes.SetDouble("pathdir", pathdir);
                WatchedAttributes.SetDouble("pathprogress", pathprogress);
                WatchedAttributes.SetDouble("pathendX", pathend.X);
                WatchedAttributes.SetDouble("pathendY", pathend.Y);
                WatchedAttributes.SetDouble("pathendZ", pathend.Z);
                WatchedAttributes.SetDouble("pathstartX", pathstart.X);
                WatchedAttributes.SetDouble("pathstartY", pathstart.Y);
                WatchedAttributes.SetDouble("pathstartZ", pathstart.Z); 
              */
        }
        public override string GetName()
        {
            return "Minecart (" + WatchedAttributes.GetString("holding") + ")";
        }

        public override void OnEntityDespawn(EntityDespawnReason despawn)
        {
            base.OnEntityDespawn(despawn);
            if (sapi != null) {
                if (despawn.reason == EnumDespawnReason.Death || despawn.reason == EnumDespawnReason.Removed||despawn.reason==EnumDespawnReason.PickedUp)
                {
                    Inventory.DropAll(ServerPos.XYZ);
                    DummyInventory di = new DummyInventory(Api, 1);
                    di[0].Itemstack = new ItemStack(Api.World.GetItem(new AssetLocation(dropitem)), 1);
                    di.DropAll(ServerPos.XYZ);
                }
                TrySaveInventory();
            }
        }
        
        public override void OnEntityLoaded()
        {
            base.OnEntityLoaded();
            if (Api is ICoreServerAPI)
            {
                TryLoadInventory();
            }
        }

        
    }
}

