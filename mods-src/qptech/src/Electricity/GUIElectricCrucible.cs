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
    class GUIElectricCrucible : GuiDialogBlockEntity
    {
        ICoreClientAPI api;
        string selection = "";
        BEElectricCrucible mycrucible;
        public GUIElectricCrucible(string dialogTitle, BlockPos blockEntityPos, ICoreClientAPI capi) : base(dialogTitle, blockEntityPos, capi)
        {
            api = capi;
        }
        public void SetupDialog(BEElectricCrucible bea)
        {
            mycrucible = bea;
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            // Just a simple 300x100 pixel box with 40 pixels top spacing for the title bar
            ElementBounds textBounds = ElementBounds.Fixed(0, 40, 600, 1200);

            // Background boundaries. Again, just make it fit it's child elements, then add the text as a child element
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(textBounds);
            string guicomponame = bea.Pos.ToString() + "Electric Crucible";
            string statustext = "SELECT INGOT FOR PRODUCTION";
            //string alertred = "<font color=\"#ffbbaa\">";//<font <color=\"#ffdddd\">>";
            double buttonx=0;
            double buttony = 100;
            double buttonwidth = 200;
            double buttonheight = 35;
             
            SingleComposer = capi.Gui.CreateCompo(guicomponame, dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar("Electric Crucible Interface", OnTitleBarCloseClicked)
                .AddRichtext(statustext, CairoFont.WhiteDetailText(), textBounds)
                
            ;
            if (bea.Recipes != null && bea.Recipes.Count > 0)
            {
                
                foreach (string key in bea.Recipes.Keys)
                {
                    
                    SingleComposer.AddSmallButton(key.ToUpperInvariant(), ()=>onMetalSelect(key), ElementBounds.Fixed(buttonx, buttony, buttonwidth, buttonheight), EnumButtonStyle.Normal, EnumTextOrientation.Center);
                    buttony += buttonheight;
                }
            }
            SingleComposer.Compose();
        }
       
        private bool onMetalSelect(string key)
        {
            mycrucible.SetOrder(key, 1, true);
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
