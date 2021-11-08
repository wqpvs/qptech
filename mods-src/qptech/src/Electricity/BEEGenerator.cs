using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;
using Vintagestory.API.Client;

namespace qptech.src
{
    class BEEGenerator:BEElectric
    {
        //how many power packets we can generate - will see if every more than one
        protected List<string> fuelCodes;   //Valid item & block codes usable as fuel
        protected int fuelTicks = 0;  //how many OnTicks a piece of fuel will last for
        int fuelCounter = 0; //counts down to use fuel
        protected bool animInit = false;
        bool usesFuel = false;          //Whether item uses fuel
        bool fueled = false;            //whether device is currently fueld
        bool usesFuelWhileOn = false;  //always use fuel, even if no load (unless turned off)
        BlockFacing fuelHopperFace;     //which face fuel is loaded from
        ILoadedSound ambientSound;
         bool requiresHeat = false;      //will check for heat to produce power
        public bool RequiresHeat => requiresHeat;
        float requiredHeat =0;      //how much heat is necessary
        BlockFacing heatFace = BlockFacing.DOWN; //which face to check for heat
        bool requiresWater = false;     //will check for water to produce power
        int waterUsage = 0;             //how much water to use up
        float waterUsePeriod = 2;       //how often to use water
        BlockFacing waterFace = BlockFacing.UP;
        bool overloadIfNoWater = false;  //will it explode if it can't find water (assuming it uses water)
        double lastwaterused = 0;
        bool generating = false;
        bool haswater = true;
        bool heated = false;
        
        public virtual float SoundLevel
        {
            get { return 0.1f; }
        }
        public override int AvailablePower()
        {
            if (!generating||!IsOn) { return 0; }
            return genPower;
        }
        public override float DisplayPercentage => isOn&&trypower ? 1 : 0;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            animInit = false;
            if (Block.Attributes != null)
            {

                
                fuelHopperFace = BlockFacing.FromCode(Block.Attributes["fuelHopperFace"].AsString("up"));
                fuelHopperFace = OrientFace(Block.Code.ToString(), fuelHopperFace);
                string[] fc = Block.Attributes["fuelCodes"].AsArray<string>();
                if (fc != null) { fuelCodes = fc.ToList<string>(); }
                fuelTicks = Block.Attributes["fuelTicks"].AsInt(1);
                usesFuel = Block.Attributes["usesFuel"].AsBool(false);
                usesFuelWhileOn = Block.Attributes["usesFuelWhileOn"].AsBool(false);
                requiredHeat = Block.Attributes["requiredHeat"].AsFloat(requiredHeat);
                if (requiredHeat > 0) { requiresHeat = true; }
                heatFace = BlockFacing.FromCode(Block.Attributes["heatFace"].AsString("down"));
                heatFace = OrientFace(Block.Code.ToShortString(), heatFace);
                waterUsage = Block.Attributes["waterUsage"].AsInt(waterUsage);
                if (waterUsage > 0) { requiresWater = true; }
                waterUsePeriod = Block.Attributes["waterUsePeriod"].AsFloat(waterUsePeriod);
                overloadIfNoWater = Block.Attributes["overloadIfNoWater"].AsBool(overloadIfNoWater);
                waterFace = BlockFacing.FromCode(Block.Attributes["waterFace"].AsString("up"));
                fuelCounter = 0;
                if (lastwaterused == 0) { lastwaterused = Api.World.Calendar.TotalHours; }
            }     
        }
        public override void OnTick(float par)
        {
            
            base.OnTick(par);
            if (isOn) {
                GeneratePower(); //Create power packets if possible, base.Ontick will handle distribution attempts
                if (requiresHeat && requiresWater && heated && !haswater && overloadIfNoWater) { DoOverload(); }
            }
            
        }
        //Attempts to generate power
        bool trypower = false;
        public virtual void GeneratePower()
        {
            trypower = DoGeneratePower();
            generating = trypower;

            if (!disableAnimations&&Api.World.Side == EnumAppSide.Client && animUtil != null)
            {
                if (!animInit)
                {
                    float rotY = Block.Shape.rotateY;
                    animUtil.InitializeAnimator(Pos.ToString() + "run", new Vec3f(0, rotY, 0));
                    animInit = true;
                }
                if (trypower)
                    animUtil.StartAnimation(new AnimationMetaData() 
                    { 
                        Animation = "run", Code = "run", AnimationSpeed = 0.7f, EaseInSpeed = 2, EaseOutSpeed = 8, Weight = 1, BlendMode = EnumAnimationBlendMode.Average 
                    });
            }

            
            ToggleAmbientSounds(trypower);
            return;
        }

        public virtual bool DoGeneratePower()
        {
            if (!isOn) { return false; }
            
            //if (Capacitor == Capacitance && !usesFuelWhileOn) { return false; }//not necessary to generate power
            if (requiresHeat && !CheckHeat()) { return false; } //perform a check for heat
            if (requiresWater && (!requiresHeat || heated) && !CheckWater()) { return false; } //check if water available
            
            if (!usesFuel) { return true; } //if we don't use fuel, we can make power
            
            //should really move fuel use to its own function
            if (fueled && fuelCounter < fuelTicks) //on going burning of current fuel item
            {
                fuelCounter++;
                return true;
            }
            //Now we begin trying to fuel
            fueled = false;fuelCounter = 0;
            BlockPos bp = Pos.Copy().Offset(fuelHopperFace);
            BlockEntity checkblock = Api.World.BlockAccessor.GetBlockEntity(bp);
            var inputContainer = checkblock as BlockEntityContainer;
            if (inputContainer == null) { return false; } //no fuel container at all
            if (inputContainer.Inventory.Empty) { return false; } //the fuel container is empty
            //check each inventory slot in the container
            for (int c = 0; c < inputContainer.Inventory.Count; c++)
            {
                ItemSlot checkslot = inputContainer.Inventory[c];
                if (checkslot == null) { continue; }
                if (checkslot.StackSize == 0) { continue; }
                
                bool match = false;
                if (checkslot.Itemstack.Item != null && fuelCodes.Contains(checkslot.Itemstack.Item.Code.ToString())) { match = true; }
                else if (checkslot.Itemstack.Block != null && fuelCodes.Contains(checkslot.Itemstack.Block.Code.ToString())) { match = true; }
                if (match&& checkslot.StackSize > 0)
                {
                    
                    checkslot.TakeOut(1);
                    checkslot.MarkDirty();
                    fueled = true;
                }
            }
            return fueled;
        }
        //generators don't receive power
        
        BlockEntityAnimationUtil animUtil
        {
            get {
                BEBehaviorAnimatable bea = GetBehavior<BEBehaviorAnimatable>();
                if (bea == null) { return null; }
                return GetBehavior<BEBehaviorAnimatable>().animUtil;
            }
        }

        

        //will check and see if there's enough water, and will use water if necessary
        bool CheckWater()
        {
            //find a block with water
            //if there is water check and see if it's time to use up some water

            //assume if we had water before (or just started) that there is water
            //TODO - need to add haswater, lastwaterused to treeattributes
            double nextwater = lastwaterused + waterUsePeriod;
            double currenttime = Api.World.Calendar.TotalHours;
            if (currenttime < nextwater && haswater) { return true; }
            haswater = false;
            lastwaterused = currenttime;
            BlockPos bp = Pos.Copy().Offset(waterFace);
            BlockEntity checkblock = Api.World.BlockAccessor.GetBlockEntity(bp);
            if (checkblock == null) { haswater = false; return false; }
            
            var checkcontainer = checkblock as BlockEntityContainer;
            if (checkcontainer != null)
            {
                for (int c = 0; c < checkcontainer.Inventory.Count; c++)
                {
                    ItemSlot checkslot = checkcontainer.Inventory[c];
                    if (checkslot == null) { continue; }
                    if (checkslot.StackSize == 0) { continue; }

                    bool match = false;
                    if (checkslot.Itemstack.Item != null && checkslot.Itemstack.Item.Code.ToString().Contains("waterportion")) { match = true; }
                    
                    if (match && checkslot.StackSize > 0)
                    {

                        checkslot.TakeOut(1);
                        if (checkslot.StackSize < 5)
                        {
                            Api.World.PlaySoundAt(new AssetLocation("sounds/waterslosh"), Pos.X, Pos.Y, Pos.Z, null, false, 8, 1);
                        }
                        else
                        {
                            Api.World.PlaySoundAt(new AssetLocation("sounds/steamburst"), Pos.X, Pos.Y, Pos.Z, null, false, 8, 1);
                        }
                        checkslot.MarkDirty();
                        haswater = true;
                        break;
                    }
                }
            }

            return haswater;
        }
        
        //will check and see if there is an appropriate heated block in order to generator power
        bool CheckHeat()
        {
            //find heated blocks - firepits, furnaces, burning coal piles
            heated = false;
            BlockPos bp = Pos.Copy().Offset(heatFace);
            BlockEntity checkblock = Api.World.BlockAccessor.GetBlockEntity(bp);
            if (checkblock == null) { return false; }
            //check for hot firepit
            var checkFirePit = checkblock as BlockEntityFirepit;
            if (checkFirePit != null)
            {
                if (checkFirePit.furnaceTemperature >= requiredHeat) { heated = true;return true; }
            }
            //check for coal piles: Note need to add check for how hot
            var checkCoalPile = checkblock as BlockEntityCoalPile;
            if (checkCoalPile != null)
            {
                if (checkCoalPile.IsBurning) { heated = true; return true; }
            }
            return false;
        }
        public void ToggleAmbientSounds(bool on)
        {
            if (Api.Side != EnumAppSide.Client) return;

            if (on)
            {
                if (ambientSound == null || !ambientSound.IsPlaying)
                {
                    ambientSound = ((IClientWorldAccessor)Api.World).LoadSound(new SoundParams()
                    {
                        Location = new AssetLocation("sounds/genloop"),
                        ShouldLoop = true,
                        Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                        DisposeOnFinish = false,
                        Volume = SoundLevel
                    });

                    ambientSound.Start();
                }
            }
            else
            {
                ambientSound?.Stop();
                ambientSound?.Dispose();
                ambientSound = null;
            }

        }

        public override void OnBlockBroken()
        {
            ambientSound?.Stop();
            ambientSound?.Dispose();
            ambientSound = null;
            base.OnBlockBroken();
        }
        public override void OnBlockRemoved()
        {
            ambientSound?.Stop();
            ambientSound?.Dispose();
            ambientSound = null;
            base.OnBlockRemoved();
        }
        public override void OnBlockUnloaded()
        {
            ambientSound?.Stop();
            ambientSound?.Dispose();
            ambientSound = null;
            base.OnBlockUnloaded();
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            
            
            if (IsOn && !heated) { dsc.AppendLine(" (NO HEAT)"); }
            else if (IsOn) { dsc.AppendLine(" (ON)"); }
            else { dsc.AppendLine(" (OFF)"); }
            //dsc.AppendLine("IN:" + inputConnections.Count.ToString() + " OUT:" + outputConnections.Count.ToString());
        }
    }
}
