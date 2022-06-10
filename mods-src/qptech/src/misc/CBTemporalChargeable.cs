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

namespace qptech.src.misc
{
    class CBTemporalChargeable:CollectibleBehavior
    {
        public CBTemporalChargeable(CollectibleObject collObj) : base(collObj)
        {

        }
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            float charge=inSlot.Itemstack.Attributes.GetFloat(BEETemporalCondenser.requiredTemporalChargeKey, 0);
            float requiredcharge=inSlot.Itemstack.Collectible.Attributes[BEETemporalCondenser.requiredTemporalChargeKey].AsFloat(0);
            if (requiredcharge != 0)
            {
                float pct = charge / requiredcharge * 100f;
                pct = (float)Math.Ceiling(pct);
                dsc.AppendLine("Temporal Charge " + pct + "%");

            }
        }
    }
}
