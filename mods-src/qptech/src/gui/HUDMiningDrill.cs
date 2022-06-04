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
using Vintagestory.API.Client;
using Vintagestory.GameContent;

namespace qptech.src.misc
{
    class HUDMiningDrill:HudElement
    {
        public ItemStack stack;
        public override bool Focusable => false;
        public override bool UnregisterOnClose => true;
        public override bool CaptureAllInputs() { return false; }
        public override bool ShouldReceiveKeyboardEvents() { return false; }
        EnumDialogType dialogType = EnumDialogType.HUD;
        public override EnumDialogType DialogType => dialogType;

        public HUDMiningDrill(ICoreClientAPI capi,ItemStack stack) : base(capi)
        {
            this.capi = capi;
            this.stack = stack;
            SetupDialog();
        }
        private void SetupDialog()
        {
            // Auto-sized dialog at the center of the screen
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            // Just a simple 300x300 pixel box
            ElementBounds textBounds = ElementBounds.Fixed(0, 0, 300, 300);

            SingleComposer = capi.Gui.CreateCompo("myAwesomeDialog", dialogBounds)
                
                .AddDynamicText("STATUS",CairoFont.WhiteDetailText(),textBounds,"status")
                .Compose()
            ;
        }
        public override void OnRenderGUI(float deltaTime)
        {
            
            //See if our stack is valid with a drill in it
            if (capi == null || stack == null || stack.Item == null||stack.StackSize==0) {
                SingleComposer.GetDynamicText("status").SetNewText("" );
                base.OnRenderGUI(deltaTime);

            }
            
            float fuel = stack.Attributes.GetFloat(ItemMiningDrill.fuelattribute, -1);
            float drill = stack.Attributes.GetFloat(ItemMiningDrill.drillheadattribute, -1);
            if (fuel == -1 || drill == -1) { SingleComposer.GetDynamicText("status").SetNewText(""); base.OnRenderGUI(deltaTime); }
            SingleComposer.GetDynamicText("status").SetNewText("Drill:"+Math.Ceiling(drill)+"% Fuel:"+fuel);

            base.OnRenderGUI(deltaTime);
        }
    }
}
