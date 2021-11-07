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
using HarmonyLib;


namespace qptech.src
{
    /// <summary>
    /// The BEEAssembler device uses energy and will create an item and place in
    /// an output chest if the relevant item is found on the input chest. Also
    /// will optionally check for the temperature of the input material (for using smelted metals etc)
    /// 
    /// If give a list of materials it will attempt to pattern match the input and output:
    /// eg: ingredient of "ingot" with an output of "game:metalplate" and a material list of
    /// "copper","tin" would look for copper & tin ingots and make the proper type of plate
    /// </summary>
    class BEEAssembler : BEEBaseDevice
    {
        protected string recipe = "game:bowl-raw";
        protected string blockoritem = "block";
        protected int outputQuantity = 1;
        protected string ingredient = "clay";
        protected string recipeSuffix = "";
        protected string ingredient_subtype = "";
        protected string ingredientSuffix = "";
        protected int inputQuantity = 4;
        protected int internalQuantity = 0; //will store ingredients virtually
        protected float animationSpeed = 0.5f;
        protected double processingTime = 10;
        protected float heatRequirement = 0;
        string readablerecipe="";
       
        public string Making => outputQuantity.ToString() + "x " + recipe + ingredient_subtype+ingredientSuffix;
        public string RM
        {
            get
            {
                string outstring = inputQuantity.ToString() + "x " + ingredient + ingredient_subtype+ingredientSuffix;
                if (heatRequirement > 0) { outstring += " at " + heatRequirement.ToString() + "°C"; }
                return outstring;
            }
        }
        public string FG
        {
            get
            {
                if (readablerecipe == "")
                {
                    AssetLocation al = new AssetLocation(recipe);
                    if (al == null) { return "error"; }
                    readablerecipe = al.GetName();

                }

                return outputQuantity.ToString()+"x "+ readablerecipe;
            }
        }
        public string Status
        {
            get
            {
                if (!IsOn) { return "OFF"; }
                else if (!IsPowered) { return "NO POWER! REQ "+RequiredFlux.ToString()+" TF"; }
                else if (deviceState == enDeviceState.MATERIALHOLD) { return "NEEDS MATERIAL"; }
                else if (deviceState== enDeviceState.RUNNING) { return "PROCESSING"; }
                else if (deviceState == enDeviceState.IDLE) { return "READY"; }
                else if (deviceState== enDeviceState.WARMUP) { return "STARTING"; }

                return "err";
            }
        }

        
        string[] materials; //List of valid materials eg: a list of metals that would work with this assembler
        public string[] Materials => materials;
        protected BlockFacing rmInputFace; //what faces will be checked for input containers
        protected BlockFacing outputFace; //what faces will be checked for output containers
        //protected BlockFacing recipeFace; //what face will be used to look for a container with the model object
         DummyInventory dummy;
        double processstarted;
        float lastheatreading = 0;
        ElectricalBlock myElectricalBlock;
        /// </summary>
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            myElectricalBlock = Block as ElectricalBlock;
            
            if (Block.Attributes != null) {
                //requiredFlux = Block.Attributes["requiredFlux"].AsInt(requiredFlux);
                rmInputFace = BlockFacing.FromCode(Block.Attributes["inputFace"].AsString("up"));
                recipeSuffix = Block.Attributes["recipeSuffix"].AsString(recipeSuffix);
                outputFace = BlockFacing.FromCode(Block.Attributes["outputFace"].AsString("down"));
                animationSpeed = Block.Attributes["animationSpeed"].AsFloat(animationSpeed);
                inputQuantity = Block.Attributes["inputQuantity"].AsInt(inputQuantity);
                outputQuantity = Block.Attributes["outputQuantity"].AsInt(outputQuantity);
                recipe = Block.Attributes["recipe"].AsString(recipe);
                ingredient = Block.Attributes["ingredient"].AsString(ingredient);
                ingredientSuffix = Block.Attributes["ingredientSuffix"].AsString(ingredientSuffix);
                rmInputFace = OrientFace(Block.Code.ToString(), rmInputFace);
                outputFace = OrientFace(Block.Code.ToString(), outputFace);
                processingTime = Block.Attributes["processingTime"].AsDouble(processingTime);
                heatRequirement = Block.Attributes["heatRequirement"].AsFloat(heatRequirement);
                blockoritem = Block.Attributes["blockoritem"].AsString(blockoritem);
                materials=Block.Attributes["materials"].AsArray<string>(materials);
            }

            dummy = new DummyInventory(api);
            
          
        }


        public void OpenStatusGUI()
        {
            ICoreClientAPI capi = Api as ICoreClientAPI;
            if (capi != null)
            {
                if (gas == null)
                {
                    gas = new GUIAssemblerStatus("Assembler Status", Pos, capi);
                    
                    gas.TryOpen();
                    gas.SetupDialog(this);
                    
                }
                else
                {
                    gas.TryClose();
                    gas.TryOpen();
                    gas.SetupDialog(this);
                }
            }
            
        }

        protected override void DoDeviceStart()
        {
            //TODO
            
            if (!IsPowered) { return; }//not enough power
            FetchMaterial();
            processstarted = Api.World.Calendar.TotalHours;      
            if (internalQuantity<inputQuantity) {  deviceState = enDeviceState.MATERIALHOLD; return; }//check for and extract the required RM
            //TODO - do we make sure there's an output container?
            if (IsPowered)
            {
                internalQuantity = 0;
                tickCounter = 0;
                deviceState = enDeviceState.RUNNING;
                
                if (!disableAnimations&& Api.World.Side == EnumAppSide.Client && animUtil != null)
                {
                    if (!animInit)
                    {
                        float rotY = Block.Shape.rotateY;
                        animUtil.InitializeAnimator(Pos.ToString() + "process", new Vec3f(0, rotY, 0));
                        animInit = true;
                    }
                    animUtil.StartAnimation(new AnimationMetaData()
                    {
                        Animation = animationName,
                        Code = animationName,
                        AnimationSpeed = animationSpeed,
                        EaseInSpeed = 2,
                        EaseOutSpeed = 4,
                        Weight = 1,
                        BlendMode = EnumAnimationBlendMode.Average
                    });
                    
                }

                //sounds/blocks/doorslide.ogg
                DoDeviceProcessing();
            }
            else { DoFailedStart(); }
        }
        
        protected override void DoDeviceProcessing()
        {
            
            if (Api.World.Calendar.TotalHours>= processingTime + processstarted)
            {
                DoDeviceComplete();
                return;
            }
            if (!IsPowered)
            {
                DoFailedProcessing();
                return;
            }
            tickCounter++;
            

        }
        GUIAssemblerStatus gas;
        protected override void DoDeviceComplete()
        {

           
            
            deviceState = enDeviceState.IDLE;
            string userecipe = recipe;
            if (ingredient_subtype != "")
            {
                userecipe += "-" + ingredient_subtype;
            }
            if (recipeSuffix != "")
            {
                userecipe += "-" + recipeSuffix;
            }
            Block outputBlock = Api.World.GetBlock(new AssetLocation(userecipe));
            Item outputItem = Api.World.GetItem(new AssetLocation(userecipe));
            if (outputBlock == null&&outputItem==null) { deviceState = enDeviceState.ERROR;return; }
            ItemStack outputStack;
            if (outputBlock!=null)
            {
                outputStack = new ItemStack(outputBlock, outputQuantity);
            }
            else
            {
                outputStack = new ItemStack(outputItem, outputQuantity);
            }
            
            dummy[0].Itemstack = outputStack;
            outputStack.Collectible.SetTemperature(Api.World, outputStack, lastheatreading);

            BlockPos bp = Pos.Copy().Offset(outputFace);
            BlockEntity checkblock = Api.World.BlockAccessor.GetBlockEntity(bp);
            IConduit checkic = checkblock as IConduit;
            if (checkic != null)
            {
                int used=checkic.ReceiveItemOffer(dummy[0], outputFace.Opposite);
                if (used > 0) { }
            }
            
                var outputContainer = checkblock as BlockEntityContainer;
                if (outputContainer != null && dummy[0].Itemstack.StackSize>0)
                {
                    WeightedSlot tryoutput = outputContainer.Inventory.GetBestSuitedSlot(dummy[0]);

                    if (tryoutput.slot != null)
                    {
                        ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, outputQuantity);

                        dummy[0].TryPutInto(tryoutput.slot, ref op);

                    }
                }
            
            if (outputStack.StackSize!=0)
            {
                //If no storage then spill on the ground
                Vec3d pos = Pos.ToVec3d();
                
                dummy.DropAll(pos);
            }
            Api.World.PlaySoundAt(new AssetLocation("sounds/doorslide"), Pos.X, Pos.Y, Pos.Z, null, false, 8, 1);
            if (!disableAnimations&& Api.World.Side == EnumAppSide.Client && animUtil != null)
            {
                
                animUtil.StopAnimation(Pos.ToString() + animationName);
            }
        }

        protected void FetchMaterial()
        {
            internalQuantity = Math.Min(internalQuantity, inputQuantity); //this shouldn't be necessary
           

            BlockPos bp = Pos.Copy().Offset(rmInputFace);
            BlockEntity checkblock = Api.World.BlockAccessor.GetBlockEntity(bp);
            var inputContainer = checkblock as BlockEntityContainer;
            if (inputContainer == null) { return; }
            if (inputContainer.Inventory.Empty) { return; }
            for (int c = 0; c < inputContainer.Inventory.Count; c++)
            {
                ItemSlot checkslot = inputContainer.Inventory[c];
                bool match = CheckCode(checkslot.Itemstack);
                if (match)
                {
                  int reqQty = Math.Min(checkslot.StackSize, inputQuantity - internalQuantity);
                  checkslot.TakeOut(reqQty);
                  internalQuantity += reqQty;
                  checkslot.MarkDirty();
                   
                }
            }
                
            
            return;
        }

        bool CheckCode(ItemStack checkstack)
        {
            
            if (checkstack == null) { return false; }
            if (checkstack.StackSize < inputQuantity) { return false; }
            bool match = false;
            Item checkitem = checkstack.Item;
            Block checkiblock = checkstack.Block;
            ingredient_subtype = "";

            if (checkitem == null && checkiblock == null) { return false; }
            if (checkitem != null)
            {
                string fcp = checkitem.FirstCodePart().ToString();
                string lcp = checkitem.LastCodePart().ToString();
                
                //no materials list so we don't need check for subtypes
                if (checkitem.Code.ToString() == ingredient) { match = true; }
                else if (ingredientSuffix != "")
                {
                    foreach (string s in materials)
                    {
                        string varcode = ingredient + s + ingredientSuffix;
                        if (checkitem.Code.ToShortString() == varcode) {
                            match = true;
                            ingredient_subtype = s;
                            break;
                        }
                    }
                }
                else if (checkitem.FirstCodePart() == ingredient && (materials == null || materials.Length == 0)) { match = true; }
                else if (checkitem.FirstCodePart().ToString() == ingredient && materials.Contains(checkitem.LastCodePart().ToString()))
                {
                    
                    ingredient_subtype = checkitem.LastCodePart();
                    match = true;
                }

            }
            else if (checkiblock != null)
            {
                if (checkiblock.Code.ToString() == ingredient) { match = true; }
                else if (ingredientSuffix != "")
                {
                    foreach (string s in materials)
                    {
                        string varcode = ingredient +"-"+ s +"-"+ ingredientSuffix;
                        AssetLocation ial = new AssetLocation(varcode);
                        Block ibl = Api.World.BlockAccessor.GetBlock(ial);
                        if (checkiblock==ibl)
                        {
                            match = true;
                            ingredient_subtype = s;
                            break;
                        }
                    }
                }
                else if (checkiblock.FirstCodePart().ToString() == ingredient && (materials == null || materials.Length == 0)) { match = true; }
                else if (checkiblock.FirstCodePart().ToString() == ingredient && materials.Contains(checkiblock.LastCodePart().ToString()))
                {
                    
                    ingredient_subtype = checkiblock.LastCodePart();
                    match = true;
                }
            }
            if (!match) { return false; }
            bool heatok = true;
            lastheatreading = checkstack.Collectible.GetTemperature(Api.World, checkstack);
            if (heatRequirement > 0 && lastheatreading < heatRequirement)
            {
                heatok = false;
            }
            return heatok;
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            internalQuantity = tree.GetInt("internalQuantity");
            processstarted = tree.GetDouble("processstarted");
            
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("internalQuantity", internalQuantity);
            tree.SetDouble("processstarted", processstarted);
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            
            dsc.AppendLine("RM   :" + internalQuantity.ToString() + "/" + inputQuantity.ToString());
            dsc.AppendLine("Make :" + recipe);
            if (deviceState == enDeviceState.RUNNING)
            {
                double timeleft = (processingTime + processstarted - Api.World.Calendar.TotalHours);
                timeleft = Math.Floor(timeleft * 100);
                
                dsc.AppendLine("Time Rem :"+timeleft.ToString());
            }
            if (heatRequirement > 0)
            {
                dsc.AppendLine("Input Item Heat must be " + heatRequirement.ToString() + "C");
            }
            if (materials != null && materials.Length > 0)
            {
                dsc.AppendLine("Usable Materials:");
                foreach (string ing in materials)
                {
                    dsc.AppendLine(ing + ",");
                }
            }
        }
        public override int ReceiveItemOffer(ItemSlot offerslot, BlockFacing onFace)
        {
            if (!IsOn) { return 0; }
            
            if (myElectricalBlock == null) { return 0; }
            //if (onFace!=rmInputFace) { return 0; }
            if (DeviceState != enDeviceState.MATERIALHOLD) { return 0; }
            if (internalQuantity >= inputQuantity) { return 0; }
            
            bool match = CheckCode(offerslot.Itemstack);
            if (match)
            {
                int reqQty = Math.Min(offerslot.Itemstack.StackSize, inputQuantity - internalQuantity);
                offerslot.TakeOut (reqQty);
                internalQuantity += reqQty;
                return reqQty;

            }

            return 0;
        }
    }
}
