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

namespace qptech.src.Electricity
{
    class BEEMixer : BEEBaseDevice,IFluidTank
    {
        //InventoryGeneric inventory;
        //Need - dry input, dry output, liquid input, liquid output
        //   - should track time sealed if necessary, have a processing speed bonus
        //   - would only use as much material as can be used, will store remaining
        //   - needs a purge function
        // Should I use an input tank instead of internal IFluidTank?
        
        int capacityLitres = 1000;
        int currentLevel = 0;
        
        public int CapacityLitres { get { return capacityLitres; } set { capacityLitres = value; } }
        public int FreeSpaceLitres => CapacityLitres - CurrentLevel;
        public bool IsFull => CurrentLevel>=CapacityLitres;

        public int CurrentLevel =>currentLevel;
        Item currentLiquid;
        Item currentOutput;
        int currentOutputQty;
        double currentBatchCompletionTime;
        Item currentDryItem;
        bool autoRunWhenFull = false;
        int capacityDry = 1000;
        public int CapacityDry => capacityDry;

        public Item CurrentItem => currentLiquid;

        public BlockPos TankPos => Pos;

        public override void Initialize(ICoreAPI api)
        {
            
            base.Initialize(api);
        }

        public int ReceiveFluidOffer(Item offeredItem, int offeredAmount, BlockPos offerFromPos)
        {
            if (IsFull) { return 0; }
            if (DeviceState == enDeviceState.RUNNING) { return 0; }
            if (CurrentItem != null && offeredItem != CurrentItem) { return 0; }
            if (CurrentItem == null) { currentLiquid = offeredItem; }
            int used = Math.Min(offeredAmount, FreeSpaceLitres);
            currentLevel += used;
            MarkDirty(true);
            return used;
        }
        public void Purge()
        {

        }
        public int TryTakeFluid(int requestedamount, BlockPos offerFromPos)
        {
            return 0;
        }

        protected override void DoDeviceStart()
        {
            foreach (BarrelRecipe br in Api.World.BarrelRecipes)
            {
                
                foreach (BarrelRecipeIngredient ing in br.Ingredients)
                {
                   
                }
            }
        }
    }
}
