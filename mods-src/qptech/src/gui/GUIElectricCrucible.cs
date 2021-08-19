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
        double sectionwidth = 280;
        double sectionheight = 1000;
        double sectionx = 0;
        double sectiony = 40;
        double buttonx;
        double buttony;
        double buttonwidth = 200;
        double buttonheight = 35;
        double buttonpad = 5;
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
       
        public void SetupStatusScreen()
        {
            //ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
            ElementBounds dialogBounds= ElementBounds.Fixed(sectionx, sectiony, 1920-40, sectionheight);


            ElementBounds textBounds = ElementBounds.Fixed(sectionx, sectiony, sectionwidth, sectionheight);

            // Background boundaries. Again, just make it fit it's child elements, then add the text as a child element
            //ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(buttonpad);
            
            // bgBounds.BothSizing = ElementSizing.FitToChildren;
            //bgBounds.WithChildren(textBounds);
            string guicomponame = mycrucible.Pos.ToString() + "Electric Crucible";
            string statustext = "";
            statustext += "<font size=\"24\" align=\"center\" >";
            statustext += "CRUCIBLE STATUS\n\n";
            statustext += "Power\n"+(int)(mycrucible.CapacitorPercentage*100)+"%";
            statustext += "\n(flux required: " + mycrucible.FluxPerTick + " flux)";
            statustext += "\n\nTemperature\n" + (int)(mycrucible.internalTempPercent * 100) + "%";
            statustext += "\n\nStorage\n" + mycrucible.UsedStorage + "/" + mycrucible.TotalStorage + "\nUnits";
            statustext += "</font>";
            //string alertred = "<font color=\"#ffbbaa\">";//<font <color=\"#ffdddd\">>";
            
            
            buttonx = (sectionwidth / 2)-buttonwidth/2;
            buttony = sectionheight-50;
            //string sigh = GuiElement.dirtTextureName; //"gui/backgrounds/soil.png"
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

        public void SetupInventoryScreen()
        {

            //FIRST ADD TITLE BAR
            double inventorywidth = sectionwidth * 2;
            double column1start = sectionx + sectionwidth + 50;
            double columnwidth = sectionwidth / 2;
            
            double columnpad = 25;
            double titleheight = 50;
            ElementBounds textBounds = ElementBounds.Fixed(column1start, sectiony, (inventorywidth - 15) / 2, titleheight);
            string text = "<font size=\"24\" align=\"left\" >";
            text += "INVENTORY(UNITS)\n\n";
            text += "</font>";
            

            //IF INVENTORY IS EMPTY, SHOW MESSAGE, DONE

            if (mycrucible.Storage == null || mycrucible.Storage.Count == 0)
            {
                text += "\n\n\nEMPTY";
                SingleComposer.AddRichtext(text, CairoFont.WhiteDetailText(), textBounds);
            }

            //OTHERWISE FIRST DRAW METALS IN FIRST COLUMN

            else
            {
                SingleComposer.AddRichtext(text, CairoFont.WhiteDetailText(), textBounds); //complete adding the title bar
                textBounds = ElementBounds.Fixed(column1start, sectiony+titleheight, (inventorywidth - 15) / 2, sectionheight- titleheight);
                text = "<font size=\"24\" align=\"left\" >";
                foreach (string key in mycrucible.Storage.Keys)
                {
                    text += key.ToUpper() + "\n";
                }
                text += "</font>";
                SingleComposer.AddRichtext(text, CairoFont.WhiteDetailText(), textBounds);
                //THEN DRAW METAL QUANTITIES IN SECOND COLUMN
                
                textBounds = ElementBounds.Fixed(column1start+columnpad+ columnwidth, sectiony + titleheight, (inventorywidth - 15) / 2, sectionheight - titleheight);
                text = "<font size=\"24\" align=\"right\" >";
                foreach (string key in mycrucible.Storage.Keys)
                {
                    text += mycrucible.Storage[key] + "\n";
                }
                text += "</font>";
                SingleComposer.AddRichtext(text, CairoFont.WhiteDetailText(), textBounds);
            }



        }
        public void SetupProductionScreen()
        {
            if (mycrucible.Recipes == null || mycrucible.Recipes.Count == 0) { return; }
            //FIRST ADD TITLE BAR
            double inventorywidth = sectionwidth * 2;
            double column1start = sectionx + sectionwidth*3 + 100;
            double columnwidth = sectionwidth / 2;

            double columnpad = 25;
            double titleheight = 50;
            ElementBounds textBounds = ElementBounds.Fixed(column1start, sectiony, (inventorywidth - 15) / 2, titleheight);
            string text = "<font size=\"24\" align=\"left\" >";
            text += "RECIPES(INGOTS)\n\n";
            text += "</font>";


            

            //OTHERWISE FIRST DRAW METALS IN FIRST COLUMN

            
            SingleComposer.AddRichtext(text, CairoFont.WhiteDetailText(), textBounds); //complete adding the title bar
            textBounds = ElementBounds.Fixed(column1start, sectiony + titleheight, (inventorywidth - 15) / 2, sectionheight - titleheight);
            text = "<font size=\"24\" align=\"left\" >";
            foreach (string key in mycrucible.Recipes.Keys)
            {
                text += key.ToUpper() + "\n";
            }
            text += "</font>";
            SingleComposer.AddRichtext(text, CairoFont.WhiteDetailText(), textBounds);
            //THEN DRAW METAL QUANTITIES IN SECOND COLUMN

            textBounds = ElementBounds.Fixed(column1start + columnpad + columnwidth, sectiony + titleheight, (inventorywidth - 15) / 2, sectionheight - titleheight);
            text = "<font size=\"24\" align=\"right\" >";
            foreach (string key in mycrucible.Recipes.Keys)
            {
                text += mycrucible.Recipes[key] + "\n";
            }
            text += "</font>";
            SingleComposer.AddRichtext(text, CairoFont.WhiteDetailText(), textBounds);
            // DRAW MAKE BUTTONS
            double ytrack = sectiony + titleheight;
            int buttonheight = 28;
            foreach (string key in mycrucible.recipes.Keys)
            {
                textBounds = ElementBounds.Fixed(column1start + columnpad*2 + (inventorywidth - 15) / 2, ytrack, columnwidth, buttonheight);
                SingleComposer.AddButton("MAKE", ()=>onMetalSelect(key,1), textBounds, EnumButtonStyle.Normal, EnumTextOrientation.Center);
                ytrack += buttonheight;
            }
        }
        
        private bool onMetalSelect(string key, int qty)
        {
            mycrucible.SetOrder(key, qty, true);
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
