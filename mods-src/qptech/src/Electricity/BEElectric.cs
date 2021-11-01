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
using qptechElectricity.API;
using Vintagestory.API.Util;
using Vintagestory.API.Client;

namespace qptech.src
{
    public class BEElectric : BlockEntity, IElectricity, ITexPositionSource
    {
        /*base class to handle electrical devices*/
        public virtual bool disableAnimations => true;
        protected int maxFlux = 16;    //how many packets that can move at once
        public TextureAtlasPosition this[string textureCode]
        {
            get
            {
                //capi.BlockTextureAtlas.Positions[Block.Textures[path].Baked.TextureSubId];
                return capi.BlockTextureAtlas.Positions[atlasBlock.Textures[UseTexture].Baked.TextureSubId];
            }
        }
        public Size2i AtlasSize => (Api as ICoreClientAPI).BlockTextureAtlas.Size;
        protected ICoreClientAPI capi;
        protected Block atlasBlock; //this block will reference a shape which will reference all used textures
        protected List<string> displayTextures; //this is a list of texture file names, must be include in the atlas block's shape
        int texno = 0; //the index number
        protected Vec3f displayOffset; //where the display will be located (if used)
        protected bool showFluxDisplay = false;
        protected float translatefactor => 16;
        protected virtual string UseTexture => displayTextures[texno];
        public int Capacitance => capacitance;//how many packets it can store
        protected int capacitance = 1;
        public virtual float DisplayPercentage => CapacitorPercentage;
        //protected int cachedCapacitance = 0;
        public int Capacitor => capacitor;  //TF currently stored
        protected int capacitor = 0;
        public float CapacitorPercentage => (float)capacitor /(float) capacitance;       
        protected bool isOn = true;        //if it's not on it won't do any power processing
        protected List<IElectricity> outputConnections; //what we are connected to output power
        protected List<IElectricity> inputConnections; //what we are connected to receive power
        protected List<IElectricity> usedconnections; //track if already traded with in a given turn (to prevent bouncing)
        protected List<BlockFacing> distributionFaces; //what faces are valid for distributing power
        protected List<BlockFacing> receptionFaces; //what faces are valid for receiving power
        
        bool distributiontick = false;
        public int MaxFlux { get { return maxFlux; } }
        

        public bool IsPowered { get { return IsOn && Capacitor > 0; } }
        public virtual bool IsOn { get { return isOn; } }
        protected bool notfirsttick = false;
        protected bool justswitched = false; //create a delay after the player switches power
        
        public BlockEntity EBlock { get { return this as BlockEntity; } }
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            //TODO need to load list of valid faces from the JSON for this stuff
            SetupIOFaces();

            if (outputConnections == null) { outputConnections = new List<IElectricity>(); }
            if (inputConnections == null) { inputConnections = new List<IElectricity>(); }

            if (Block.Attributes == null) { api.World.Logger.Error("ERROR BEE INITIALIZE HAS NO BLOCK"); return; }
            maxFlux = Block.Attributes["maxFlux"].AsInt(maxFlux);
            maxFlux *= 10;
            capacitance = Block.Attributes["capacitance"].AsInt(Capacitance);

            RegisterGameTickListener(OnTick, 75);
            notfirsttick = false;
            if (api is ICoreClientAPI)
            {
                capi = api as ICoreClientAPI;
            }
            displayTextures = new List<string>();
            string displaytextureatlasblockname = "machines:dummygauge";
            displaytextureatlasblockname = Block.Attributes["atlasBlock"].AsString(displaytextureatlasblockname);
            atlasBlock = api.World.GetBlock(new AssetLocation(displaytextureatlasblockname));
            float[] displayoffset = Block.Attributes["displayOffset"].AsArray<float>();
            if (displayoffset == null||displayoffset.Length!=3)
            {
                displayOffset = new Vec3f(4, 10, 3);
                displayOffset = displayOffset / translatefactor;
            }
            else
            {
                displayOffset = new Vec3f(displayoffset);
                displayOffset = displayOffset / translatefactor;
            }
            string[] displaytexturelist = Block.Attributes["displayTextures"].AsArray<string>();
            if (displaytexturelist == null || displaytexturelist.Length==0)
            {
                displayTextures = new List<string>();
                displayTextures.Add("roundgauge-0");
                displayTextures.Add("roundgauge-10");
                displayTextures.Add("roundgauge-20");
                displayTextures.Add("roundgauge-30");
                displayTextures.Add("roundgauge-40");
                displayTextures.Add("roundgauge-50");
                displayTextures.Add("roundgauge-60");
                displayTextures.Add("roundgauge-70");
                displayTextures.Add("roundgauge-80");
                displayTextures.Add("roundgauge-90");
                displayTextures.Add("roundgauge-100");
            }
            else
            {
                displayTextures = displayTextures.ToList<string>();
            }
            showFluxDisplay = Block.Attributes["showFluxDisplay"].AsBool(showFluxDisplay);
        }

        //attempt to load power distribution and reception faces from attributes, and orient them to this blocks face if necessary
        public virtual void SetupIOFaces()
        {
            string[] cfaces = { };

            if (Block.Attributes == null)
            {
                distributionFaces = BlockFacing.HORIZONTALS.ToList<BlockFacing>();
                receptionFaces = BlockFacing.HORIZONTALS.ToList<BlockFacing>();

                return;
            }
            if (!Block.Attributes.KeyExists("receptionFaces")) { receptionFaces = BlockFacing.ALLFACES.ToList<BlockFacing>(); }
            else
            {
                cfaces = Block.Attributes["receptionFaces"].AsArray<string>(cfaces);
                receptionFaces = new List<BlockFacing>();
                foreach (string f in cfaces)
                {
                    receptionFaces.Add(OrientFace(Block.Code.ToString(), BlockFacing.FromCode(f)));
                }
            }

            if (!Block.Attributes.KeyExists("distributionFaces")) { distributionFaces = BlockFacing.ALLFACES.ToList<BlockFacing>(); }
            else
            {
                cfaces = Block.Attributes["distributionFaces"].AsArray<string>(cfaces);
                distributionFaces = new List<BlockFacing>();
                foreach (string f in cfaces)
                {
                    distributionFaces.Add(OrientFace(Block.Code.ToString(), BlockFacing.FromCode(f)));
                }
            }

        }
        
        public virtual void FindConnections()
        {
            ClearConnections(); 
            FindInputConnections();
            FindOutputConnections();

        }
        protected void ClearConnections()
        {
            if (inputConnections != null)
            {
                foreach (IElectricity ie in inputConnections)
                {
                    if (ie != null) { ie.RemoveConnection(this); }
                }
                inputConnections = new List<IElectricity>();
            }
            if (outputConnections != null)
            {
                foreach (IElectricity ie in outputConnections)
                {
                    if (ie != null) { ie.RemoveConnection(this); }
                }
                outputConnections = new List<IElectricity>();
            }
        }
        protected virtual void FindInputConnections()
        {
            //BlockFacing probably has useful stuff to do this right
            
            if (receptionFaces == null) return;

            foreach (BlockFacing bf in receptionFaces)
            {
                BlockPos bp = Pos.Copy().Offset(bf);

                BlockEntity checkblock = Api.World.BlockAccessor.GetBlockEntity(bp);
                var bee = checkblock as IElectricity;
                if (bee == null) { continue; }
                if (bee.TryOutputConnection(this) && !inputConnections.Contains(bee)) { inputConnections.Add(bee); }

            }
        }

        protected virtual void FindOutputConnections()
        {
            //BlockFacing probably has useful stuff to do this right

            foreach (BlockFacing bf in distributionFaces)
            {
                BlockPos bp = Pos.Copy().Offset(bf);
                BlockEntity checkblock = Api.World.BlockAccessor.GetBlockEntity(bp);
                var bee = checkblock as IElectricity;
                if (bee == null) { continue; }
                if (bee.TryInputConnection(this) && !outputConnections.Contains(bee)) { outputConnections.Add(bee); }

            }
        }

        //Allow devices to connection to each other

        //API
        public virtual bool TryInputConnection(IElectricity connectto)
        {
            if (inputConnections == null) { inputConnections = new List<IElectricity>(); }
            Vec3d vector = connectto.EBlock.Pos.ToVec3d() - Pos.ToVec3d();
            BlockFacing bf = BlockFacing.FromVector(vector.X, vector.Y, vector.Z);
            if (receptionFaces == null) { return false; }
            if (!receptionFaces.Contains(bf)) { return false; }
            if (!inputConnections.Contains(connectto)) { inputConnections.Add(connectto); MarkDirty(); }
            return true;
        }
        //API
        public virtual bool TryOutputConnection(IElectricity connectto)
        {
            if (outputConnections == null) { outputConnections = new List<IElectricity>(); }
            Vec3d vector = connectto.EBlock.Pos.ToVec3d() - Pos.ToVec3d();
            BlockFacing bf = BlockFacing.FromVector(vector.X, vector.Y, vector.Z);
            if (distributionFaces == null) { return false; }
            if (!distributionFaces.Contains(bf)) { return false; }
            if (!outputConnections.Contains(connectto)) { outputConnections.Add(connectto); MarkDirty(); }
            return true;
        }

        //Tell a connection to remove itself
        //API
        public virtual void RemoveConnection(IElectricity disconnect)
        {
            inputConnections.Remove(disconnect);
            outputConnections.Remove(disconnect);
        }

        public override void OnBlockBroken()
        {
            base.OnBlockBroken();

            foreach (IElectricity bee in inputConnections) { bee.RemoveConnection(this); }
            foreach (IElectricity bee in outputConnections) { bee.RemoveConnection(this); }
        }
        public virtual void OnTick(float par)
        {
            if (!notfirsttick)
            {
                FindConnections();
                notfirsttick = true;
            }

            if (isOn && distributiontick) {
                DistributePower();
                //FlushCapacitorCache();
                usedconnections = new List<IElectricity>(); //clear record of connections for next tick
            }
            distributiontick = !distributiontick;
            justswitched = false;
            if (Api is ICoreClientAPI && showFluxDisplay)
            {
                UpdateFluxDisplay();
            }
        }
        protected virtual void UpdateFluxDisplay()
        {
            float pcttracker = DisplayPercentage;
            if (pcttracker > 1) { pcttracker = 1; }
            if (pcttracker < 0) { pcttracker = 0; }
            int newtexno = (int)((float)(displayTextures.Count - 1) * pcttracker);

            if (newtexno != texno)
            {
                texno = newtexno;
                this.MarkDirty(true);
            }
        }

        protected virtual float DisplayRotation()
        {
            float rot = 0;
            switch (Block.LastCodePart())
            {
                case "east": rot = 270; break;
                case "south": rot = 180; break;
                case "west": rot = 90; break;
                case "north": rot = 0; break;
            }
            return rot;
        }
        protected virtual Vec3f DisplayOffset() //display offset but translated directionally
        {
            switch (Block.LastCodePart())
            {
                case "east": return displayOffset;
                case "south": return displayOffset;
                case "west": return displayOffset;
            }
            return displayOffset;
        }
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {

            if (!showFluxDisplay) { return base.OnTesselation(mesher, tessThreadTesselator); }
            Shape displayshape = capi.TesselatorManager.GetCachedShape(new AssetLocation("machines:block/metal/electric/roundgauge0"));


            MeshData meshdata;
            capi.Tesselator.TesselateShape("roundgauge0" + Pos.ToString(), displayshape, out meshdata, this);



            meshdata.Translate(DisplayOffset());
            meshdata.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, GameMath.DEG2RAD * DisplayRotation(), 0);


            mesher.AddMeshData(meshdata);
            return base.OnTesselation(mesher, tessThreadTesselator);
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if (IsOn) { dsc.AppendLine("Turned On (right click with screwdriver or hammer to turn on/off)"); }
            else { dsc.AppendLine("Turned Off (right click with screwdriver or hammer to turn on/off)"); }

            dsc.AppendLine("Stored Temporal Flux " + Capacitor.ToString() + " of " + Capacitance.ToString());
            
            //dsc.AppendLine("IN:" + inputConnections.Count.ToString() + " OUT:" + outputConnections.Count.ToString());
        }

        //Used for other power devices to offer this device some energy returns how much power was used
        //API
        public virtual int ReceivePacketOffer(IElectricity from, int inFlux) //eg 2
        {
            if (usedconnections == null) { usedconnections = new List<IElectricity>(); }
            if (!isOn) { return 0; }//Not even on
            
            if (inFlux <= 0) { return 0; }
            if (Capacitor >= Capacitance) { return 0; }//already full
            inFlux = Math.Min(inFlux, MaxFlux); //can only move a certain amount of TF - eg 2
            int useflux = Math.Min(inFlux, Capacitance - Capacitor); //2
            useflux = Math.Max(useflux, 0);
            ChangeCapacitor(useflux);

            usedconnections.Add(from);
            if (useflux != 0) { MarkDirty(); }//not zero should be dirty
            return useflux;//return 2
        }
        
        //Attempt to send out power (can be overridden for devices that only use power)
        public virtual void DistributePower()
        {
            // bunch of checks to see if we can give power
            if (Capacitor == 0) { return; }
            if (usedconnections == null) { usedconnections = new List<IElectricity>(); }
            if (!isOn) { return; } //can't generator power if off
            if (outputConnections == null) { return; } //nothing hooked up
            if (outputConnections.Count == 0) { return; }

            //figure out who needs power
            List<IElectricity> tempconnections = new List<IElectricity>();
            int powerreq = 0;
            foreach (IElectricity ie in outputConnections)
            {
                int np = ie.NeedPower();
                if (np == 0) { continue; }
                powerreq += np;
                tempconnections.Add(ie);
            }
            if (powerreq == 0) { return; } //Don't need to distribute any power
            bool gavepower = false;
            //cap the powerrequest to our max TF, by the number of requests
            powerreq = Math.Min(powerreq, tempconnections.Count * maxFlux);
            //distribute what power we can
            //If we have more power than is requested, just go through and give power
            if (Capacitor >= powerreq)
            {
               foreach (IElectricity ie in tempconnections)
                {
                    int offer = ie.ReceivePacketOffer(this,  Math.Min(Capacitor, maxFlux));
                    if (offer > 0) { ChangeCapacitor(-offer);gavepower=true; }
                }
               if (gavepower) { MarkDirty(true); }
               return;
            }

            //Not enough power to go around, have to divide it up
            int eachavail = Capacitor / tempconnections.Count;
            int leftover = Capacitor % tempconnections.Count; //remainder
            foreach (IElectricity ie in tempconnections)
            {
                int offer = ie.ReceivePacketOffer(this, eachavail);
                if (offer == 0) { continue; }
                gavepower = true;
                ChangeCapacitor(-offer);
                if (leftover > 0)
                {
                    offer = ie.ReceivePacketOffer(this, leftover);
                    if (offer > 0)
                    {
                        leftover -= offer;
                        ChangeCapacitor(-offer);
                    }
                }
            }
            if (gavepower) { MarkDirty(true); }

        }

        public virtual void DoOverload()
        {
            ////BOOOOM!
            if (!IsOn) { return; }
            EnumBlastType blastType=EnumBlastType.OreBlast;
            var iswa = Api.World as IServerWorldAccessor;
            Api.World.BlockAccessor.SetBlock(0, Pos);
            if (iswa!=null)
            {
                iswa.CreateExplosion(Pos, blastType, 4, 15);
                isOn = false;
            }
            
        }

        public virtual void TogglePower()
        {
            if (justswitched) { return; }
            isOn = !isOn;
            justswitched = true;
            Api.World.PlaySoundAt(new AssetLocation("sounds/electriczap"), Pos.X, Pos.Y, Pos.Z, null, false, 8, 1);
        }

        public virtual int NeedPower()
        {
            int needs = 0;
            if (isOn)
            {
                needs = Capacitance - Capacitor;
                if (needs < 0) { needs = 0; }
                needs = Math.Min(needs, MaxFlux);
            }
            return needs;
            
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);


            //if (type == null) type = defaultType; // No idea why. Somewhere something has no type. Probably some worldgen ruins
            capacitor = tree.GetInt("capacitor");
            isOn = tree.GetBool("isOn");
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            
            tree.SetInt("capacitor", Capacitor);
            tree.SetBool("isOn", isOn);
        }
        public void ChangeCapacitor(int amt)
        {
           // if (distributiontick && amt > 0) { cachedCapacitance += amt; }
           // else
            //{
                capacitor += amt;
                
            //}
            capacitor = Math.Max(Capacitor, 0);
            capacitor = Math.Min(Capacitance, Capacitor);
        }
        /*void FlushCapacitorCache()
        {
            capacitor += cachedCapacitance;
            capacitor = Math.Max(Capacitor, 0);
            capacitor = Math.Min(Capacitance, Capacitor);
            cachedCapacitance = 0;
        }*/
        //Take a block code (that ends in a cardinal direction) and a BlockFacing,
        //and rotate it, returning the appropriate blockfacing
        public static BlockFacing OrientFace(string checkBlockCode, BlockFacing toChange)
        {
            if (!toChange.IsHorizontal) { return toChange; }
            if (checkBlockCode.EndsWith("east"))
            {

                toChange = toChange.GetCW();
            }
            else if (checkBlockCode.EndsWith("south"))
            {
                toChange = toChange.GetCW().GetCW();
            }
            else if (checkBlockCode.EndsWith("west"))
            {
                toChange = toChange.GetCCW();
            }
    
            return toChange;
        }

        
    }
}
