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
using Vintagestory.API.MathTools;
using qptech.src.extensions;
using System.Text.RegularExpressions;

namespace qptech.src.misc
{
    class PathLimitedEntity : Entity
    {
        Vec3d pathstart;
        Vec3d pathend;
        double pathdir = 1;
        double pathprogress = 0;
        double pathspeed {
            get{
                if (pathend.Y > pathstart.Y) { return 0.075; }
                else if (pathend.Y < pathstart.Y) { return 0.125; }
                return 0.1;
            }
        }
        bool moving = false;
        CollisionTester collTester = new CollisionTester();
        EntityPartitioning ep;
        BlockFacing heading = BlockFacing.NORTH;
        Vec3d pathpos => new Vec3d(GameMath.Lerp(pathstart.X, pathend.X, pathprogress) + 0.5, GameMath.Lerp(pathstart.Y, pathend.Y, pathprogress), GameMath.Lerp(pathstart.Z, pathend.Z, pathprogress) + 0.5);
        string pathcodecontains = "rails";
        string dropitem = "machines:creature-testentity";
        ICoreServerAPI sapi;
        InventoryGeneric inventory;
        public virtual InventoryGeneric Inventory => inventory;
        int inventorysize = 1;
        bool startpathset = false;
        public override bool ApplyGravity => false;
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

                //List<byte> datalist = sapi.WorldManager.SaveGame.GetData<List<byte>>(GetEntityStorageKey());
                
                byte[]data = WatchedAttributes.GetBytes(inventorykey);
                ITreeAttribute loadtree = null;
                if (data != null)
                {
                    
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
            WatchedAttributes.SetBytes(inventorykey, data);
            WatchedAttributes.MarkPathDirty(inventorykey);
            //SAVE TO FILE ApiExtensions.SaveDataFile<List<byte>>(Api, GetChestFilename(player), datalist);
            //sapi.WorldManager.SaveGame.StoreData<List<byte>>(GetEntityStorageKey(), datalist);
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
            if (CheckOtherCart()) { return; }
            pathprogress += pathdir * pathspeed;
           
            if (pathprogress >=1) {
                pathprogress = 1;pathstart = pathend;
                
                FindPath();
            }
            ServerPos.SetPos(pathpos);

        }

        //check for other carts at destination
        // should pause if there's a cart not heading towards us, or reverse direction if it is heading our way
        protected virtual bool CheckOtherCart()
        {
            Entity checkentity = ep.GetNearestEntity(pathend, 1, (e) => {
                if (e.EntityId == EntityId) { return false; }
                if (!(e is PathLimitedEntity)) { return false; }
                return true;
            });
            
            if (checkentity == null) { return false; }
            PathLimitedEntity other = checkentity as PathLimitedEntity;
            //if (other.heading == heading.Opposite) { heading = heading.Opposite; FindPath(); }
            heading = heading.Opposite; FindPath();
            return true;
        }

        protected virtual void FindPath()
        {
            if (!HandleInventory()) { return; }
            moving = false;
            pathprogress = 0;
            BlockPos currentP = pathstart.AsBlockPos;
            
            Block currentBlock;
            
            currentBlock = Api.World.BlockAccessor.GetBlock(currentP);
            
            if (!isRail(currentBlock)) {
                for (int c = 0; c < 1; c++)
                {
                    
                    currentP.Y -= 1;
                    currentBlock = Api.World.BlockAccessor.GetBlock(currentP);
                    if (isRail(currentBlock)) { break; }
                }
                if (!isRail(currentBlock))
                {
                    moving = false; Die(); return;
                }
            }
            
            BlockFacing newheading = heading;
            
            BlockPos outpos;
            //pick new destination based on block we are currently in, where we were headed, and if the possible destination blocks were rails
            moving = CheckExit(currentBlock, heading, currentP, out newheading, out outpos);
            
           
            if (moving)
            {
                if (currentBlock.Attributes != null)
                {
                    string switchblock = currentBlock.Attributes["railswitch"].AsString(null);
                    if (switchblock != null)
                    {
                        Block newrail = Api.World.GetBlock(new AssetLocation(switchblock));
                        if (newrail != null)
                        {
                            Api.World.BlockAccessor.SetBlock(newrail.BlockId, currentP);
                        }
                    }
                }
                startpathset = true;
                pathprogress = 0;
                
                pathend = outpos.ToVec3d();
                heading = newheading;

                if (newheading == BlockFacing.SOUTH)
                {
                    //ServerPos.SetYaw(0 * 0.0174533f);
                    ServerPos.SetYaw(180* 0.0174533f);
                }
                else if (newheading == BlockFacing.EAST)
                {
                    //ServerPos.SetYaw(90 * 0.0174533f);
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
        public  bool isRail(Block testblock)
        {
            if (testblock.FirstCodePart().Contains(pathcodecontains)) { return true; }
            return false;
        }
        public bool isRail(BlockPos checkpos)
        {
            Block testblock = Api.World.BlockAccessor.GetBlock(checkpos);
            if (isRail(testblock)) { return true; }
            return false;
        }
        public virtual bool CheckExit(Block startblock,BlockFacing orgheading, BlockPos orgpos, out BlockFacing newheading, out BlockPos newgoal)
        {     
            newheading = orgheading;
            newgoal = orgpos;
            string fcp = startblock.FirstCodePart();
            string lcp = startblock.LastCodePart();
            Dictionary<BlockFacing, BlockPos> exits = new Dictionary<BlockFacing, BlockPos>();
            if (!isRail(startblock)) { return false; }
            if (lcp.Contains("flat_ns"))
            {
                TryAdd(orgpos,BlockFacing.NORTH, ref exits);
                TryAdd(orgpos,BlockFacing.SOUTH, ref exits);
                
            }
            else if (lcp.Contains("flat_we"))
            {
                TryAdd(orgpos, BlockFacing.EAST, ref exits);
                TryAdd(orgpos, BlockFacing.WEST, ref exits);
                
            }
            else if (lcp.Contains("curved_es"))
            {
                TryAdd(orgpos, BlockFacing.EAST, ref exits);
                TryAdd(orgpos, BlockFacing.SOUTH, ref exits);  
            }
            else if (lcp.Contains("curved_sw"))
            {
                TryAdd(orgpos, BlockFacing.SOUTH, ref exits);
                TryAdd(orgpos, BlockFacing.WEST, ref exits);
            }
            else if (lcp.Contains("curved_wn"))
            {
                TryAdd(orgpos, BlockFacing.WEST, ref exits);
                TryAdd(orgpos, BlockFacing.NORTH, ref exits);
            }
            else if (lcp.Contains("curved_ne"))
            {
                TryAdd(orgpos, BlockFacing.NORTH, ref exits);
                TryAdd(orgpos, BlockFacing.EAST, ref exits);
            }
            else if (fcp.Contains("vertical"))
            {
                if (lcp.Contains("north"))
                {
                    TryAdd(orgpos.Copy().Offset(BlockFacing.UP), BlockFacing.NORTH, ref exits);
                    TryAdd(orgpos, BlockFacing.SOUTH, ref exits);
                }
                else if (lcp.Contains("south"))
                {
                    TryAdd(orgpos.Copy().Offset(BlockFacing.UP), BlockFacing.SOUTH, ref exits);
                    TryAdd(orgpos, BlockFacing.NORTH, ref exits);
                }
                else if (lcp.Contains("east"))
                {
                    TryAdd(orgpos.Copy().Offset(BlockFacing.UP), BlockFacing.EAST, ref exits);
                    TryAdd(orgpos, BlockFacing.WEST, ref exits);
                }
                else if (lcp.Contains("west"))
                {
                    TryAdd(orgpos.Copy().Offset(BlockFacing.UP), BlockFacing.WEST, ref exits);
                    TryAdd(orgpos, BlockFacing.EAST, ref exits);
                }
            }
            //find an exit, first choice is an exit matching the heading, otherwise use the first available
            //no exit, fail!
            if (exits.Count == 0) { return false; }
            //if we can keep going in same direction do so
            if (exits.ContainsKey(orgheading)) { newgoal = exits[heading]; return true; }
            //if there's only one exit, go that way
            if (exits.Keys.Count == 1)
            {
                newheading = exits.Keys.First();
                newgoal = exits[newheading];
                return true;
            }
            //almost last choice is to go in a non-opposite direction
            foreach (BlockFacing key in exits.Keys)
            {
                if (key != orgheading.Opposite)
                {
                    newheading = key;
                    newgoal = exits[newheading];
                    return true;
                }
            }
            //fine well reverse then (searching probably isn't necessary at this point)
            foreach (BlockFacing key in exits.Keys)
            {
                if (key == orgheading.Opposite)
                {
                    newheading = key;
                    newgoal = exits[newheading];
                    return true;
                }
            }
            //this is a really odd case
            return false;
        }
        void TryAdd(BlockPos orgpos, BlockFacing addface, ref Dictionary<BlockFacing, BlockPos> exits)
        {
            BlockPos checkpos = orgpos.Copy().Offset(addface);
            if (isRail(checkpos)) { exits[addface] = checkpos; }
            else
            {
                checkpos.Offset(BlockFacing.DOWN);
                if (isRail(checkpos)) { exits[addface] = checkpos; }
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

