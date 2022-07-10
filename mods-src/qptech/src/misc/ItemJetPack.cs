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

namespace qptech.src.misc
{
    class ItemJetPack : Item
    {
        
        double thrust = 0.075;
        double lateralthrust = 0.01;
        double throttleSpeed = 0.1; //max speed in x sec
        double throttlePercent = 0; //current throttle
        double maxSpeed = 0.5; //cut off thrust if speed too high
        double ceiling = 300; //cut off any thrust after this

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            //base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            handling = EnumHandHandling.Handled;
            
        }
        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            
            if (byEntity.Pos.Y > ceiling) { return false; }
            
            throttlePercent = Math.Min(secondsUsed, throttleSpeed)/throttleSpeed;
            
                
            Vec3d vecthrust = Vec3d.Zero;
            bool hovermode = false;
            if (byEntity.Pos.Motion.Y < maxSpeed && byEntity.HeadPitch<-0.05f)
            {
                byEntity.Pos.Motion.Y = thrust*throttlePercent;
            }
            //else if (byEntity.HeadPitch > 0.05f)
            //{
                //let gravity do its thing
            //}
            else
            {
                hovermode = true;
            }
            vecthrust += byEntity.Controls.WalkVector.Normalize() * lateralthrust*throttlePercent;
            byEntity.Pos.Motion += vecthrust;
            if (hovermode)
            {
                byEntity.Pos.Motion.Y *= 0.1;
            }
                //byEntity.Pos.Motion=    thrust * throttlePercent*byEntity;
            
            
            return true;
        }
    }
}
