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
        public virtual bool hasInventory => true;
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
                GetBehavior<EntityBehaviorPassivePhysics>().OnPhysicsTickCallback = onPhysicsTickCallback;
                ep = api.ModLoader.GetModSystem<EntityPartitioning>();
                Vec3d begin = ServerPos.XYZ;
                begin.X = Math.Floor(begin.X);
                begin.Y = Math.Floor(begin.Y);
                begin.Z = Math.Floor(begin.Z);
                pathstart = begin;
                FindPath();
            }
        }
        private void onPhysicsTickCallback(float dtFac)
        {
            

            
        }
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

        long msinteract;
        public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode)
        {
            base.OnInteract(byEntity, itemslot, hitPosition, mode);
            if (Api.World.ElapsedMilliseconds + 200 < msinteract) { return; }
            msinteract = Api.World.ElapsedMilliseconds+200;
            if (Api is ICoreServerAPI) { 
                if (moving) { Stop(); }
                else { Start(hitPosition); }
            }
        }

        public override void OnGameTick(float dt)
        {
            base.OnGameTick(dt);
            if (Api is ICoreServerAPI)
            {
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
            //Check below for hopper
            BlockPos p = ServerPos.AsBlockPos;
            p.Y -= 2;
            BlockEntity b = Api.World.BlockAccessor.GetBlockEntity(p);
            if (p == null)
            {
                return true;
            }
            BlockEntityItemFlow flow = b as BlockEntityItemFlow;
            if (flow != null&&flow.Inventory!=null)
            {
                
                foreach (ItemSlot slot in flow.Inventory)
                {
                    //temporary just add some magic coal
                    if (slot.Empty)
                    {
                        slot.Itemstack= new ItemStack(Api.World.GetItem(new AssetLocation("game:charcoal")), 1);
                        slot.MarkDirty();
                        return false;
                        
                    }
                }
            }
            return true;
        }

        public override void ToBytes(BinaryWriter writer, bool forClient)
        {
            base.ToBytes(writer, forClient);
            if (!forClient)
            {
                WatchedAttributes.SetBool("moving", moving);
                WatchedAttributes.SetString("heading", heading.ToString());
                
            }
        }
        public override void FromBytes(BinaryReader reader, bool fromServer)
        {
            base.FromBytes(reader, fromServer);
            moving = WatchedAttributes.GetBool("moving");
            string tryheading = WatchedAttributes.GetString("heading");
            heading = BlockFacing.FromCode(tryheading);
            
        }
    }
}
