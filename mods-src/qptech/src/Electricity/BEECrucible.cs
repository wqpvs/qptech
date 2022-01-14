using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using qptech.src.networks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Client;
using Vintagestory.API.Server;

namespace qptech.src
{
    /// <summary>
    /// Simple electric crucible - gets nuggets, heats, and exports ingots
    /// BEElectric Crucible will be the larger version with complex interface etc
    /// </summary>
    class BEECrucible:BEEBaseDevice
    {
        //check input chest for nuggets
        //if found (and enough there) look up the combustiblePropsByType.meltingPoint
        //check if enough heat supplied (firepit, or Industiral Process?)
        //if there is enough then take the nuggets begin processing
        //output ingot when done, heated appropriately
        int capacityIngots = 1;
        protected BlockFacing rmInputFace;
        protected BlockFacing outputFace;
        ItemStack currentbatch;
        double pourStartTime = 0;
        double pourEndTime => pourStartTime + processingTime;
        public ItemStack CurrentBatch=>currentbatch;
        public enum enMode { ALLOY,SINGLE}
        enMode processingMode=enMode.ALLOY;
        public enMode ProcessingMode => processingMode;
        public override bool showToggleButton => true;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (Block.Attributes != null)
            {
                capacityIngots = Block.Attributes["capacityIngots"].AsInt(capacityIngots);
                rmInputFace = BlockFacing.FromCode(Block.Attributes["inputFace"].AsString("up"));
                outputFace = BlockFacing.FromCode(Block.Attributes["outputFace"].AsString("down"));
                rmInputFace = OrientFace(Block.Code.ToString(), rmInputFace);
                outputFace = OrientFace(Block.Code.ToString(), outputFace);
                
            }
        }

        protected override void DoDeviceStart()
        {
            IBlockEntityContainer container = Api.World.BlockAccessor.GetBlockEntity(Pos.Copy().Offset(rmInputFace)) as IBlockEntityContainer;
            if (container == null) { return; }
            if (container.Inventory == null) { return; }
            if (container.Inventory.Empty) { return; }
            List<ItemStack> stacks = new List<ItemStack>();
            List<string> materials = new List<string>();
            foreach (ItemSlot slot in container.Inventory)
            {
                //checks for valid items
                if (slot == null || slot.Empty) { continue; }
                if (slot.Itemstack == null || slot.Itemstack.StackSize <= 0) { continue; }
                if (slot.Itemstack.Item == null) { continue; }
                if (slot.Itemstack.Item.CombustibleProps == null) { continue; }
                if (slot.Itemstack.Item.CombustibleProps.SmeltedStack == null) { continue; }
                AssetLocation mat = slot.Itemstack.Item.CombustibleProps.SmeltedStack.Code;
                int qty = slot.Itemstack.StackSize;
                float temp = slot.Itemstack.Collectible.GetTemperature(Api.World, slot.Itemstack);
                if (!materials.Contains(mat.ToString())) { materials.Add(mat.ToString()); }
                if (temp < slot.Itemstack.Item.CombustibleProps.MeltingPoint) { continue; }
                
                                
                
                stacks.Add(slot.Itemstack);
            }
            
            //If there is nothing valid, then don't do anything
            if (stacks.Count() <= 0) { deviceState = enDeviceState.MATERIALHOLD;MarkDirty(); return; }
            //Figure out what alloys can be made (if any)
            
            AlloyRecipe canmake = GetMatchingAlloy(Api.World, stacks.ToArray());
            if (canmake != null&&processingMode==enMode.ALLOY)
            {
                double outputqty = canmake.GetTotalOutputQuantity(stacks.ToArray());
                double remainder = Math.Abs(outputqty - Math.Round(outputqty));
                if (remainder > 0.001)
                {
                    return;
                }
                int units = (int)Math.Round(outputqty);
                currentbatch = new ItemStack(canmake.Output.ResolvedItemstack.Item,units);
                deviceState = enDeviceState.RUNNING;
                pourStartTime = Api.World.ElapsedMilliseconds;
                DoPourFX();
                //TODO Properly clear only relevant stacks
                foreach (ItemSlot slot in container.Inventory)
                {
                    //checks for valid items
                    if (slot == null || slot.Empty) { continue; }
                    if (slot.Itemstack == null || slot.Itemstack.StackSize <= 0) { continue; }
                    if (slot.Itemstack.Item == null) { continue; }
                    if (slot.Itemstack.Item.CombustibleProps == null) { continue; }
                    if (slot.Itemstack.Item.CombustibleProps.SmeltedStack == null) { continue; }
                    AssetLocation mat = slot.Itemstack.Item.CombustibleProps.SmeltedStack.Code;
                    int qty = slot.Itemstack.StackSize;
                    float temp = slot.Itemstack.Collectible.GetTemperature(Api.World, slot.Itemstack);
                    if (!materials.Contains(mat.ToString())) { materials.Add(mat.ToString()); }
                    if (temp < slot.Itemstack.Item.CombustibleProps.MeltingPoint) { continue; }
                    slot.Itemstack = null;

                }
                (container as BlockEntity).MarkDirty();
                MarkDirty();
                return;
            }
            if (processingMode == enMode.SINGLE)
            {
                MatchedSmeltableStack mss = BlockSmeltingContainer.GetSingleSmeltableStack(stacks.ToArray());
                if (mss == null) { deviceState = enDeviceState.MATERIALHOLD; MarkDirty(); return; }
                double remainder = Math.Abs(mss.stackSize - Math.Floor(mss.stackSize));
                if (remainder > 0.001) { return; }
                currentbatch = mss.output;
                currentbatch.StackSize = (int)mss.stackSize;
                DoPourFX();

                //Find metal quantity: combustibleprops.smeltedstack.resolveditemstack.stacksize/combustibleprops.smeltedratio
                deviceState = enDeviceState.RUNNING;
                pourStartTime = Api.World.ElapsedMilliseconds;
                foreach (ItemSlot slot in container.Inventory)
                {
                    //checks for valid items
                    if (slot == null || slot.Empty) { continue; }
                    if (slot.Itemstack == null || slot.Itemstack.StackSize <= 0) { continue; }
                    if (slot.Itemstack.Item == null) { continue; }
                    if (slot.Itemstack.Item.CombustibleProps == null) { continue; }
                    if (slot.Itemstack.Item.CombustibleProps.SmeltedStack == null) { continue; }
                    AssetLocation mat = slot.Itemstack.Item.CombustibleProps.SmeltedStack.Code;
                    int qty = slot.Itemstack.StackSize;
                    float temp = slot.Itemstack.Collectible.GetTemperature(Api.World, slot.Itemstack);
                    if (!materials.Contains(mat.ToString())) { materials.Add(mat.ToString()); }
                    if (temp < slot.Itemstack.Item.CombustibleProps.MeltingPoint) { continue; }
                    slot.Itemstack = null;

                }
                (container as BlockEntity).MarkDirty();
                MarkDirty(); return;
            }
            if (deviceState != enDeviceState.MATERIALHOLD) { deviceState = enDeviceState.MATERIALHOLD; MarkDirty(); return; }
        }
        void DoPourFX()
        {
            if (Api is ICoreClientAPI) { 

                ILoadedSound pambientSound = ((IClientWorldAccessor)Api.World).LoadSound(new SoundParams()
                {
                    Location = new AssetLocation("sounds/pourmetal"),
                    ShouldLoop = false,
                    Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                    DisposeOnFinish = true,
                    Volume = 2,
                    Range = 15
                });

                pambientSound.Start();
            }
            else
            {
                (Api as ICoreServerAPI).Network.BroadcastBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, clientplaysound, null);
            }
        }
        
        void FillFromBatch()
        {
            if (Api.World.ElapsedMilliseconds < pourEndTime) { return; }
            IBlockEntityContainer outcontainer = Api.World.BlockAccessor.GetBlockEntity(Pos.Copy().Offset(outputFace)) as IBlockEntityContainer;
            if (outcontainer != null)
            {
                foreach (ItemSlot tryslot in outcontainer.Inventory)
                {
                    if (tryslot != null)
                    {

                        bool isemptyslot = true;
                        if (tryslot.Itemstack != null && tryslot.StackSize > 0) { isemptyslot = false; }

                        if (isemptyslot)
                        {
                            currentbatch.StackSize--;
                            if (currentbatch.Block != null) { tryslot.Itemstack = new ItemStack(currentbatch.Block, 1); }
                            else { tryslot.Itemstack = new ItemStack(currentbatch.Item, 1); }
                            (outcontainer as BlockEntity).MarkDirty();
                            
                            pourStartTime = Api.World.ElapsedMilliseconds;
                            if (currentbatch.StackSize <= 0)
                            {
                                currentbatch = null;
                                deviceState = enDeviceState.IDLE;
                            }
                            MarkDirty();
                            break;
                        }
                        bool ismatchingslot = false;
                        if (tryslot.Itemstack.Block != null && tryslot.Itemstack.Block == currentbatch.Block) { ismatchingslot = true; }
                        else if (tryslot.Itemstack.Item!=null && tryslot.Itemstack.Item == currentbatch.Item) { ismatchingslot = true; }
                        if (ismatchingslot)
                        {
                            currentbatch.StackSize--;
                            tryslot.Itemstack.StackSize++;
                            (outcontainer as BlockEntity).MarkDirty();
                            pourStartTime = Api.World.ElapsedMilliseconds;
                            if (currentbatch.StackSize <= 0)
                            {
                                currentbatch = null;
                                deviceState = enDeviceState.IDLE;
                            }
                            MarkDirty();
                            break;
                        }
                    }
                }
                    
            }
            
        }
        protected override void UsePower()
        {
            
            
            if (HasCurrentBatch())
            {
                FillFromBatch();
            }
            else
            {
                deviceState = enDeviceState.IDLE;
            }
            base.UsePower();
        }

        bool HasCurrentBatch()
        {
            bool hascurrentbatch = true;
            if (currentbatch == null) { hascurrentbatch = false; }
            else if (currentbatch.StackSize <= 0) { hascurrentbatch = false; }
            else if (currentbatch.Item == null && currentbatch.Block == null) { hascurrentbatch = false; }
            return hascurrentbatch;
        }


public AlloyRecipe GetMatchingAlloy(IWorldAccessor world, ItemStack[] stacks)
        {
            List<AlloyRecipe> alloys = Api.GetMetalAlloys();
            if (alloys == null) return null;

            for (int j = 0; j < alloys.Count; j++)
            {
                if (alloys[j].Matches(stacks))
                {
                    return alloys[j];
                }
            }

            return null;
        }

        public override string GetStatusUI()
        {
            string status = "<strong>Crucible Status</strong><br>";
            status += "<strong>MODE:" + processingMode.ToString() + "</strong>";
            if (processingMode == enMode.ALLOY) { status += " (will make alloys)<br"; }
            else if (processingMode == enMode.SINGLE) { status += " (will not process alloys)<br"; }
            if (currentbatch != null && currentbatch.Item != null)
            {
                status += "Pouring " + currentbatch.StackSize + " ingots of " + currentbatch.Item.GetHeldItemName(currentbatch);
            }
            else if (currentbatch != null && currentbatch.Block != null)
            {
                status += "Pouring " + currentbatch.StackSize + " ingots of " + currentbatch.Block.GetHeldItemName(currentbatch);
            }
                status += base.GetStatusUI();
            
            return status;
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("processingMode", (int)processingMode);
            tree.SetItemstack("currentbatch", currentbatch);

        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            int pm = tree.GetInt("processingMode");
            processingMode = (enMode)tree.GetInt("processingMode");
            currentbatch = tree.GetItemstack("currentbatch");
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if (processingMode == enMode.ALLOY) { dsc.Append("Alloy Mode (will make alloys)"); }
            if (processingMode == enMode.SINGLE) { dsc.Append("Single Mode (will not make alloys)"); }
        }
        public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
        {
            base.OnReceivedClientPacket(fromPlayer, packetid, data);
            if (packetid == (int)enPacketIDs.ToggleMode)
            {
                if (processingMode == enMode.ALLOY) { processingMode = enMode.SINGLE; }
                else { processingMode = enMode.ALLOY; }
                MarkDirty();
            }
        }

        int clientplaysound = 999900001;

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            base.OnReceivedServerPacket(packetid, data);
            if (packetid == clientplaysound)
            {
                DoPourFX();
            }
        }
    }
}
