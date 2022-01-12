using System;
using System.Linq;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace qptech.src
{
    class GUIBEElectric:GuiDialogBlockEntity
    {
        ICoreClientAPI api;
        BEElectric thisbee;
        public GUIBEElectric(string dialogTitle, BlockPos blockEntityPos, ICoreClientAPI capi) : base(dialogTitle,blockEntityPos,capi)
        {
            api = capi;
        }
        public void SetupDialog(BEElectric bea)
        {
            thisbee = bea;
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
            ElementBounds textBounds = ElementBounds.Fixed(0, 100, 600, 600);
            ElementBounds buttonBounds = ElementBounds.Fixed(0, 40, 100, 20);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(textBounds,buttonBounds);
            string guicomponame = bea.Pos.ToString()+"Processor";
            string statustext = bea.GetStatusUI();
            string powerbutton = "TURN OFF";
            if (!bea.IsOn) { powerbutton = "TURN ON"; }
            
            SingleComposer = capi.Gui.CreateCompo(guicomponame, dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar("Machine Status", OnTitleBarCloseClicked)
                .AddRichtext(statustext, CairoFont.WhiteDetailText(), textBounds)
                .AddButton(powerbutton, onTogglePower, buttonBounds, CairoFont.WhiteDetailText())
                
                .Compose()
                
            ;

        }
        public virtual bool onTogglePower()
        {
            thisbee.TogglePowerButton();
            TryClose();
            return true;
        }

        public override bool TryOpen()
        {
            
            return base.TryOpen();
        }
        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }
     

    }
}
