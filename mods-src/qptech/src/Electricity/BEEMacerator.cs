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
using Vintagestory.API.Client;
using Vintagestory.API.Server;

namespace qptech.src
{
    /// <summary>
    /// The Macerator - Also covers the washplant
    /// Takes power and an input chest of materials (and eventually water) and
    /// outputs various other materials, either specific materials or pattern
    /// matched materials. Can set odds and quantities that would be dropped in
    /// the macerationrecipes.json
    /// Set the machinename in the relevant blocktype json to specify what machines recipes are for
    /// default/unspecified will be macerator (so if you don't set this your machine will use any
    /// recipe that is specified for machinename macerator or any recipe that has no machinename set)
    /// 
    /// macerationrecipes.json example:
    ///	"muddygravel": [                           <- this is the input item, then we have an array of outputs
	///	{                                          <- start of the entry (you can have more than one thing output per input item)
	///		"outputmaterial": "game:clay-blue",    <- this would output blue clay
	///		"outputquantity": 4,                   <- outputs 1-4 blue clay
	///		"odds": 50,                            <- 50% chance of outputing any clay
	///		"type": "PARTIAL",                     <- match type partial - means the input will be pattern matched, but the output will be the a specific item
	///		"machinename":  "washplant"            <- the machine name (default is "macerator") - so in the blocktypes json file for washplant i set a machine name of washplant in the attributes to pickup this recipe
    /// },                                         <- end of this output item - you could add another item after this to be outputted
	/// ],                                         <- end of this recipe
    /// 
    /// Also lumbermill is kinda hacked in - logs using the swap type will always output planks
    /// 
    /// </summary>
    class BEEMacerator:BEEBaseDevice
    {
        string materialInputFace = "up";
        string materialOutputFace = "down";
        protected BlockFacing rmInputFace=>BlockFacing.FromCode(materialInputFace); //what faces will be checked for input containers
        protected BlockFacing outputFace=> BlockFacing.FromCode(materialOutputFace);
        CollectibleObject workingitem;
        protected string machinename = "macerator";
        
        public string MachineName => machinename;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (Block.Attributes != null)
            {
                
                machinename = Block.Attributes["machinename"].AsString(machinename);
                materialInputFace = Block.Attributes["materialInputFace"].AsString(materialInputFace);
                materialOutputFace = Block.Attributes["materialOutputFace"].AsString(materialOutputFace);

            }
            SetupAnimation();
        }
        protected override void DoDeviceStart()
        {
            if (Capacitor < requiredFlux) { DoFailedStart(); }
            tickCounter = 0;
            TryStart();
                        
        }
        protected override void DoDeviceComplete()
        {
            deviceState = enDeviceState.IDLE;
            BlockPos bp = Pos.Copy().Offset(outputFace);
            BlockEntity checkblock = Api.World.BlockAccessor.GetBlockEntity(bp);
            var outputContainer = checkblock as BlockEntityContainer;
            if (Api.World is IServerWorldAccessor)
            {
                DummyInventory dummy = new DummyInventory(Api);

                List<ItemStack> outputitems = MacerationRecipe.GetMacerate(workingitem, Api,MachineName);
                foreach (ItemStack outitem in outputitems)
                {
                    if (outitem == null) { continue; }
                    dummy[0].Itemstack = outitem;
                    //no output conatiner, spitout stuff
                    if (outputContainer != null)
                    {

                        bool stoptrying = false;
                        int safetycounter = 0;
                        while (!stoptrying)
                        {
                            WeightedSlot tryoutput = outputContainer.Inventory.GetBestSuitedSlot(dummy[0]);

                            if (tryoutput.slot != null)
                            {
                                ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, dummy[0].StackSize);

                                dummy[0].TryPutInto(tryoutput.slot, ref op);
                                tryoutput.slot.MarkDirty();
                                if (dummy[0]==null) { stoptrying = true; }
                                else if (dummy[0].StackSize == 0) { stoptrying = true; }
                            }
                            else { stoptrying = true; }
                            safetycounter++;
                            if (safetycounter > 24) { stoptrying = true; }
                        }
                    }
                    Vec3d pos = bp.ToVec3d();

                    dummy.DropAll(pos);
                }
            }
        }

        void TryStart()
        {
            BlockPos bp = Pos.Copy().Offset(rmInputFace);
            BlockEntity checkblock = Api.World.BlockAccessor.GetBlockEntity(bp);
            deviceState = enDeviceState.MATERIALHOLD;
            if (Api.World is IServerWorldAccessor)
            {
                workingitem = null;
                var inputContainer = checkblock as BlockEntityContainer;
                if (inputContainer == null) { return; }
                if (inputContainer.Inventory.Empty) { return; }
                for (int c = 0; c < inputContainer.Inventory.Count; c++)
                {
                    ItemSlot checkslot = inputContainer.Inventory[c];
                    if (checkslot == null) { continue; }
                    if (checkslot.StackSize == 0) { continue; }
                    
                    Item checkitem = checkslot.Itemstack.Item;
                    Block checkiblock = checkslot.Itemstack.Block;

                    if (checkitem == null && checkiblock == null)
                    {
                        continue;
                    }
                    CollectibleObject co;
                    if (checkitem != null) { co = checkitem as CollectibleObject; }
                    else { co = checkiblock as CollectibleObject; }

                    if (!MacerationRecipe.CanMacerate(co, Api,MachineName)) { continue; }
                    workingitem = co;
                    //Item has been set, need to pull one item from the stack
                    deviceState = enDeviceState.RUNNING;
                    checkslot.TakeOut(1);
                    checkslot.MarkDirty();
                    break;
                }
                
            }
        }

        protected override void DoDeviceProcessing()
        {
            base.DoDeviceProcessing();
        }




        void SetupAnimation()
        {
            if (Api.World.Side == EnumAppSide.Client && animUtil != null)
            {
                if (!animInit)
                {
                    float rotY = Block.Shape.rotateY;
                    animUtil.InitializeAnimator(Pos.ToString()+ "process", new Vec3f(0, rotY, 0));
                    animInit = true;
                }
                animUtil.StartAnimation(new AnimationMetaData()
                {
                    Animation = animationName,
                    Code = animationName,
                    AnimationSpeed = 1,
                    EaseInSpeed = 1,
                    EaseOutSpeed = 1,
                    Weight = 1,
                    BlendMode = EnumAnimationBlendMode.Add
                });

            }
        }
        
    }
    public class MacerationRecipe
    {
        public string machinename = "macerator";
        public string outputmaterial;
        public int outputquantity=1;
        public int inputquantity = 1;
        public float odds=1;
        
        public enTypes type = enTypes.SWAP;
        /// <summary>
        /// enTypes
        /// DIRECT - change one specific item into another specific item
        /// SWAP   - change one item into another by swapping part of its Code
        /// ORESWAP - swap part of the item code, plus swap the metal type using the orelookups list (eg: malachite->copper)
        /// PARTIAL - if partial match of input item, out a specific item
        /// 
        /// </summary>
        public enum enTypes {DIRECT,SWAP,ORESWAP,PARTIAL};

        
        static Dictionary<string, List<MacerationRecipe>> maceratelist;
        static Dictionary<string, string> orelookups;

        public MacerationRecipe()
        {
         
        }
        public MacerationRecipe(string materialout,int quantityout,float oddsout)
        {
            outputmaterial = materialout;
            outputquantity = quantityout;
            inputquantity = 1;
            odds = oddsout;
        }
        public MacerationRecipe(string materialout, int quantityout, float oddsout,int inputquantityout,string machinenameout)
        {
            outputmaterial = materialout;
            outputquantity = quantityout;
            inputquantity = inputquantityout;
            machinename = machinenameout;
            odds = oddsout;
        }
        public static bool CanMacerate(CollectibleObject co,ICoreAPI api,string machinename)
        {
            //TODO: change this to "CanMacerate", add a FindMacerate that returns items:
            //   - from the maceratelist directly - adding extra output based on RNG
            //   - generically where possible - eg stone block to stone gravel etc
            
            if (maceratelist == null) { LoadMacerateLists(api); }
            if (co == null) { return false; }
            if (co.FirstCodePart() == "ore") { return true; }
            if (co.ToString().Contains("log")&&machinename=="logsplitter") { return true; }
            if (co.FirstCodePart() == "nugget") { return false; } //disabling nugget processing for now
            if (maceratelist.ContainsKey(co.FirstCodePart())) {
                bool result = maceratelist[co.FirstCodePart()].Any(val => val.machinename==machinename);
                return result; 
            }
            if (maceratelist.ContainsKey(co.Code.ToString())) {
                bool result = maceratelist[co.Code.ToString()].Any(val => val.machinename == machinename);
                return result;
            }
            
            return false;
        }

        public static List<ItemStack> GetMacerate(CollectibleObject co, ICoreAPI api,string machinename)
        {
            if (machinename == "") { machinename = "macerator"; }
            List<ItemStack> outputstack = new List<ItemStack>();
            if (!CanMacerate(co, api,machinename)) { return outputstack; }
            string fcp = co.FirstCodePart();
            string fullcode = co.Code.ToString();
            Random rand = new Random();
            /*if (fullcode.Contains("log")&&machinename=="logsplitter")
            {
                Item outputItem = api.World.GetItem(new AssetLocation("game:firewood"));
                ItemStack itmstk = new ItemStack(outputItem, 16);
                outputstack.Add(itmstk);
            }*/
            if (fcp == "ore")
            {
                ItemOre oreitem = co as ItemOre;
                if (oreitem != null)
                {
                    string outitemcode = "game:nugget-";
                    bool metalfound = false;
                    foreach (string outmetal in orelookups.Keys)
                    {
                        if (fullcode.Contains(outmetal))
                        {
                            outitemcode += outmetal;
                            metalfound = true;
                            break;
                        }
                    }
                    if (metalfound)
                    {
                        int mu = oreitem.Attributes["metalUnits"].AsInt(1);
                        
                        
                        Item outputItem = api.World.GetItem(new AssetLocation(outitemcode));
                        ItemNugget nuggie = outputItem as ItemNugget;
                        
                        int outitemqty = mu/5  + rand.Next(0, 5);

                        if (outputItem != null)
                        {
                            ItemStack itmstk = new ItemStack(outputItem, outitemqty);
                            outputstack.Add(itmstk);
                        }
                    }

                }
            }
            if (maceratelist.ContainsKey(fcp)) {
                foreach (MacerationRecipe mr in maceratelist[fcp])
                {

                    if (mr.type == enTypes.DIRECT) { continue; }
                    if (mr.machinename != machinename&&mr.machinename!="*") { continue; }
                    double roll = rand.NextDouble() * 100;
                    if (!(mr.odds == 100 || roll <= mr.odds)) { continue; }
                    string al = fullcode;
                    if (mr.type == enTypes.SWAP)
                    {
                        al = al.Replace(fcp, mr.outputmaterial);
                    }
                    else if (mr.type == enTypes.ORESWAP)
                    {
                        foreach (string nativemetal in orelookups.Keys)
                        {
                            if (fullcode.Contains(nativemetal))
                            {
                                al = al.Replace(nativemetal, orelookups[nativemetal]);
                                al = al.Replace("game", "machines");//THIS IS A HACK PLZ FIX
                            }
                        }
                    }
                    else if (mr.type== enTypes.PARTIAL)
                    {
                        al = mr.outputmaterial;
                    }
                    int outqty = mr.outputquantity;
                    if (mr.odds != 100) { outqty = rand.Next(1, mr.outputquantity + 1); }
                    Block outputBlock = api.World.GetBlock(new AssetLocation(al));
                    Item outputItem = api.World.GetItem(new AssetLocation(al));
                    if (outputBlock != null)
                    {
                        ItemStack itmstk = new ItemStack(outputBlock, outqty);
                        outputstack.Add(itmstk);
                    }
                    if (outputItem != null)
                    {
                        ItemStack itmstk = new ItemStack(outputItem, outqty);
                        outputstack.Add(itmstk);
                    }
                }
            }
            if (maceratelist.ContainsKey(fullcode)) { 
            foreach (MacerationRecipe mr in maceratelist[fullcode])
            {

                if (mr.type == enTypes.SWAP||mr.type==enTypes.PARTIAL) { continue; }
                if (mr.machinename != machinename && mr.machinename != "*") { continue; }
                double roll = rand.NextDouble() * 100;
                if (!(mr.odds == 100 || roll <= mr.odds)) { continue; }
                string al = mr.outputmaterial;

                int outqty = mr.outputquantity;
                if (mr.odds != 100) { outqty = rand.Next(1, mr.outputquantity + 1); }
                Block outputBlock = api.World.GetBlock(new AssetLocation(al));
                Item outputItem = api.World.GetItem(new AssetLocation(al));
                if (outputBlock != null)
                {
                    ItemStack itmstk = new ItemStack(outputBlock, outqty);
                    outputstack.Add(itmstk);
                }
                if (outputItem != null)
                {
                    ItemStack itmstk = new ItemStack(outputItem, outqty);
                    outputstack.Add(itmstk);
                }
            }
            }
           
            return outputstack;
        }
        static void LoadMacerateLists(ICoreAPI api)
        {
            //maceratelist = new Dictionary<string, List<MacerationRecipe>>();
            maceratelist = api.Assets.TryGet("qptech:config/macerationrecipes.json").ToObject<Dictionary<string, List<MacerationRecipe>>>();
            orelookups = api.Assets.TryGet("qptech:config/orelookups.json").ToObject<Dictionary<string, string>>();

        }

    }
}
