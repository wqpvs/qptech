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

namespace qptech.src
{
    class BEEBlastFurnace:BEEBaseDevice
    {
        //List<AlloyRecipe> alloys = world.Alloys;
        /*if (stack.Collectible.CombustibleProps?.SmeltedStack != null && stack.Collectible.CombustibleProps.MeltingPoint > 0)
            {
                stackSize *= stack.Collectible.CombustibleProps.SmeltedStack.StackSize;
                stackSize /= stack.Collectible.CombustibleProps.SmeltedRatio;
                stack = stack.Collectible.CombustibleProps.SmeltedStack.ResolvedItemstack;
            }*/
        /*
         * Blast Furnace/Electric Crucible will handle its own inventory, heating
         * - input inventory - need at least 3 if doing alloys - needs to be marked as inputable
         * - power input - if multiblock how do we handle that?
         * - processing inventory (?) (may just abstract it)
         * - output inventory - needs to be marked as outputable
         * - max processing temp
         * - UI - inventories, power status, processing status - alloy selection?
         * - need to lookup various soft metals nuggets->ingots
         * - need to lookup alloys - but how to select desired alloy - UI?
         */
        DummyInventory inventory;
        public override void Initialize(ICoreAPI api)
        {
            List<AlloyRecipe> alloys = api.GetMetalAlloys();

            base.Initialize(api);
        }
    }
}
