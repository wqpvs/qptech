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
            if (bea == null) { return; }
            SetupStatusScreen();
            SetupInventoryScreen();
            SetupProductionScreen();
            SingleComposer.Compose();
        }
        public void SetupInventoryScreen()
        {

        }
        public void SetupProductionScreen()
        {

        }
        public void SetupStatusScreen()
        {
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            
            int sectionwidth = 280;
            int sectionheight = 1000;
            ElementBounds textBounds = ElementBounds.Fixed(0, 40, sectionwidth, sectionheight);

            // Background boundaries. Again, just make it fit it's child elements, then add the text as a child element
            //ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(5);
            
            // bgBounds.BothSizing = ElementSizing.FitToChildren;
            //bgBounds.WithChildren(textBounds);
            string guicomponame = mycrucible.Pos.ToString() + "Electric Crucible";
            string statustext = "";
            statustext += "<font size=\"24\" align=\"center\" >";
            statustext += "Power\n"+(int)(mycrucible.CapacitorPercentage*100)+"%";
            statustext += "\n(flux required: " + mycrucible.FluxPerTick + " flux)";
            statustext += "\n\nTemperature\n" + (int)(mycrucible.internalTempPercent * 100) + "%";
            statustext += "\n\nStorage\n" + mycrucible.UsedStorage + "/" + mycrucible.TotalStorage + "\nUnits";
            statustext += "</font>";
            //string alertred = "<font color=\"#ffbbaa\">";//<font <color=\"#ffdddd\">>";
            
            double buttonwidth = 200;
            double buttonheight = 35;
            double buttonpad=5;
            double buttonx = (sectionwidth / 2)-buttonwidth/2;
            double buttony = sectionheight-50;
            string sigh = GuiElement.dirtTextureName; //"gui/backgrounds/soil.png"
            ElementBounds buttonbounds = ElementBounds.Fixed(buttonx, buttony, buttonwidth, buttonheight);
            //C:\Users\quent\AppData\Roaming\Vintagestory\assets\game\textures\gui\backgrounds
            SingleComposer = capi.Gui.CreateCompo(guicomponame, dialogBounds)
                //.AddShadedDialogBG(bgBounds)
                //.AddImageBG(bgBounds,"castiron")
                //.AddImageBG(bgBounds, "environment/stars-bg.png")
                .AddImageBG(bgBounds, "gui/backgrounds/mainmenu2.png",0)
                
                //.AddDialogTitleBar("Electric Crucible Interface", OnTitleBarCloseClicked)
                .AddRichtext(statustext, CairoFont.WhiteDetailText(), textBounds)
                //.AddSmallButton("Close", OnCloseButton, ElementBounds.Fixed(buttonx, buttony, buttonwidth, buttonheight), EnumButtonStyle.Normal, EnumTextOrientation.Center)
                .AddButton("EXIT",OnCloseButton,buttonbounds,EnumButtonStyle.Normal,EnumTextOrientation.Center)
            ;
            if (mycrucible.Status == BEElectricCrucible.enStatus.PRODUCING)
            {
                buttony -= buttonheight+buttonpad;
                buttonbounds = ElementBounds.Fixed(buttonx, buttony, buttonwidth, buttonheight);
                SingleComposer.AddButton("HALT",onHaltProduction, buttonbounds, EnumButtonStyle.Normal, EnumTextOrientation.Center);
            }
            string text = "TURN OFF";
            if (!mycrucible.IsOn) { text = "TURN ON"; }
            buttony -= buttonheight+buttonpad;
            buttonbounds = ElementBounds.Fixed(buttonx, buttony, buttonwidth, buttonheight);
            SingleComposer.AddButton(text, onTogglePower, buttonbounds, EnumButtonStyle.Normal, EnumTextOrientation.Center);

        }
       public void SetupReadyDialog()
        {
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            // Just a simple 300x100 pixel box with 40 pixels top spacing for the title bar
            ElementBounds textBounds = ElementBounds.Fixed(40, 40, 1500, 2000);

            // Background boundaries. Again, just make it fit it's child elements, then add the text as a child element
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(textBounds);
            string guicomponame =mycrucible.Pos.ToString() + "Electric Crucible";
            
            string statustext = "SELECT INGOT FOR PRODUCTION";
            statustext += "\nHeat at " + (int)(mycrucible.internalTempPercent * 100) + "%";
            //string alertred = "<font color=\"#ffbbaa\">";//<font <color=\"#ffdddd\">>";
            double buttonx = 0;
            double buttony = 100;
            double buttonwidth = 200;
            double buttonheight = 35;

            SingleComposer = capi.Gui.CreateCompo(guicomponame, dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar("Electric Crucible Interface", OnTitleBarCloseClicked)
                .AddRichtext(statustext, CairoFont.WhiteDetailText(), textBounds)

            ;
            if (mycrucible.Recipes != null && mycrucible.Recipes.Count > 0)
            {

                foreach (string key in mycrucible.Recipes.Keys)
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
            TryClose();
            return true;
           
        }
        public override bool TryOpen()
        {

            return base.TryOpen();
        }
        public bool OnCloseButton()
        {
            TryClose();
            return true;
        }
        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }
        public bool onHaltProduction()
        {
            TryClose();
            return true;
        }
        public bool onTogglePower()
        {
            mycrucible.ButtonTogglePower();
            TryClose();
            return true;
        }
    }
}
