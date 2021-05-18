﻿using System;
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
    class BEEMacerator:BEEBaseDevice
    {
        protected BlockFacing rmInputFace=BlockFacing.FromCode("up"); //what faces will be checked for input containers
        protected BlockFacing outputFace = BlockFacing.FromCode("down");
        CollectibleObject workingitem;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            SetupAnimation();
        }
        protected override void DoDeviceStart()
        {
            if (Capacitor < requiredAmps) { DoFailedStart(); }
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

                List<ItemStack> outputitems = MacerationRecipe.GetMacerate(workingitem, Api);
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

                    if (!MacerationRecipe.CanMacerate(co, Api)) { continue; }
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
                    animUtil.InitializeAnimator("process", new Vec3f(0, rotY, 0));
                    animInit = true;
                }
                animUtil.StartAnimation(new AnimationMetaData()
                {
                    Animation = "process",
                    Code = "process",
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
        public string outputmaterial;
        public int outputquantity=1;
        public float odds=1;
        public enTypes type = enTypes.SWAP;
        /// <summary>
        /// enTypes
        /// DIRECT - change one specific item into another specific item
        /// SWAP   - change one item into another by swapping part of its Code
        /// ORESWAP - swap part of the item code, plus swap the metal type using the orelookups list (eg: malachite->copper)
        /// </summary>
        public enum enTypes {DIRECT,SWAP,ORESWAP};

        
        static Dictionary<string, List<MacerationRecipe>> maceratelist;
        static Dictionary<string, string> orelookups;

        public MacerationRecipe()
        {
         
        }
        public MacerationRecipe(string materialout,int quantityout,float oddsout)
        {
            outputmaterial = materialout;
            outputquantity = quantityout;
            odds = oddsout;
        }
        public static bool CanMacerate(CollectibleObject co,ICoreAPI api)
        {
            //TODO: change this to "CanMacerate", add a FindMacerate that returns items:
            //   - from the maceratelist directly - adding extra output based on RNG
            //   - generically where possible - eg stone block to stone gravel etc
            if (maceratelist == null) { LoadMacerateLists(api); }
            if (co == null) { return false; }
            if (co.FirstCodePart() == "ore") { return true; }
            if (co.FirstCodePart() == "nugget") { return false; } //disabling nugget processing for now
            if (maceratelist.ContainsKey(co.FirstCodePart())) { return true; }
            if (maceratelist.ContainsKey(co.Code.ToString())) { return true; }
            return false;
        }

        public static List<ItemStack> GetMacerate(CollectibleObject co, ICoreAPI api)
        {
            
            List<ItemStack> outputstack = new List<ItemStack>();
            if (!CanMacerate(co, api)) { return outputstack; }
            string fcp = co.FirstCodePart();
            string fullcode = co.Code.ToString();
            Random rand = new Random();
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
                        int outitemqty = oreitem.Attributes["metalUnits"].AsInt(1)+rand.Next(0, 5);
                        
                        Item outputItem = api.World.GetItem(new AssetLocation(outitemcode));
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

                    double roll = rand.NextDouble() * 100;
                    if (!(mr.odds == 100 || roll <= mr.odds)) { continue; }
                    string al = fullcode;

                    al = al.Replace(fcp, mr.outputmaterial);
                    if (mr.type == enTypes.ORESWAP)
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

                if (mr.type == enTypes.SWAP) { continue; }

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