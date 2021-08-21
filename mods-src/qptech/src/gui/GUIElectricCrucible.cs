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
        bool isopen = false;
        //public Vintagestory.API.Common.Action<bool> idkaction ;
        BEElectricCrucible mycrucible;

     
        public GUIElectricCrucible(string dialogTitle, BlockPos blockEntityPos, ICoreClientAPI capi) : base(dialogTitle, blockEntityPos, capi)
        {
            api = capi;
        }
        public void SetupDialog(BEElectricCrucible bea)
        {
            mycrucible = bea;
            if (bea == null) { return; }
            ElementBounds dialogBounds = ElementBounds.Fixed(sectionx, sectiony, 256, 256);
            string guicomponame = mycrucible.Pos.ToString() + "Electric Crucible";
            SingleComposer = capi.Gui.CreateCompo(guicomponame, dialogBounds);
            SetupPowerScreen();
            //SetupStatusScreen();
            //SetupInventoryScreen();
            //SetupProductionScreen();
            SingleComposer.Compose();
            isopen = true;
        }
        public void SetupPowerScreen()
        {
            ElementBounds dialogBounds = ElementBounds.Fixed(sectionx, sectiony, 256, 256);
            double screenx = sectionx + 21;
            double screeny = sectiony + 128;
            double screenwidth = 82;
            double screenheight = 68;
            ElementBounds screenBounds = ElementBounds.Fixed(screenx, screeny, screenwidth, screenheight);
            //x try and add a switch
            SingleComposer.AddInteractiveElement(new GuiElementSwitch(api, TogglePower, screenBounds, 68, 0));
            activepaneltexture = "electrical panel.png";
            SingleComposer.AddDynamicCustomDraw(dialogBounds, testdraw, "testbar");
            double switchx=11;
            double switchy = 177;
            string statustext = "";
            if (!mycrucible.IsOn)
            {
                switchy = 211;
                statustext = "<font align=\"center\"></font>";//should probably just not draw it but welp
            }
            else if (!mycrucible.IsPowered)
            {
                statustext = "<font align=\"center\">NO POWER</font>";
            }
            else
            {
                statustext = "<font style=\"bold\" align =\"center\">Charge at " + mycrucible.CapacitorPercentage * 100 + "%\nPower Usage:"+mycrucible.FluxPerTick+" Flux</font>";
            }
            ElementBounds switchBounds = ElementBounds.Fixed(switchx, switchy, 51, 23);
            SingleComposer.AddDynamicCustomDraw(switchBounds, powerswitch, "powerswitch");
            //Draw status info
            screenwidth = 205-6;screenheight = 55-6;screenx = 36;screeny = 100;
            screenBounds = ElementBounds.Fixed(screenx, screeny, screenwidth, screenheight);
            SingleComposer.AddRichtext(statustext, CairoFont.WhiteDetailText(), screenBounds);
            //Draw title
            screenwidth = 116;screenheight = 15;
            screenx = sectionx+71;
            screeny = sectiony+21;
            screenBounds = ElementBounds.Fixed(screenx, screeny, screenwidth, screenheight);
            statustext = "<font align=\"center\" color=#4a2200>Electric Crucible</font>";

            SingleComposer.AddRichtext(statustext, CairoFont.WhiteDetailText(), screenBounds);

            if (mycrucible.Storage != null && mycrucible.Storage.Count > 0)
            {

                //Now let's try and draw the inventory panel
                double screenxoffest = 256;
                dialogBounds = ElementBounds.Fixed(sectionx + screenxoffest, sectiony, 256, 256);
                GEDrawTexture gdt = new GEDrawTexture(capi, dialogBounds, "big status panel.png");
                SingleComposer.AddDynamicCustomDraw(dialogBounds, gdt.OnDraw);

                //Draw title
                screenwidth = 116; screenheight = 15;
                screenx = sectionx + 71 + screenxoffest;
                screeny = sectiony + 21;
                screenBounds = ElementBounds.Fixed(screenx, screeny, screenwidth, screenheight);
                statustext = "<font align=\"center\" color=#4a2200>INVENTORY</font>";
                SingleComposer.AddRichtext(statustext, CairoFont.WhiteDetailText(), screenBounds);
                screenwidth = 200; screenheight = 160;
                screenx = sectionx + screenxoffest + 28;
                screeny = sectiony + 55;
                screenBounds = ElementBounds.Fixed(screenx, screeny, screenwidth, screenheight);
                statustext = "<font align=\"left\" >";
                foreach (string key in mycrucible.Storage.Keys)
                {
                    statustext += key + "\n";
                }
                statustext += "</font>";
                SingleComposer.AddRichtext(statustext, CairoFont.WhiteDetailText(), screenBounds);
                screenx = sectionx + 160 + screenwidth / 2;
                screenBounds = ElementBounds.Fixed(screenx, screeny, screenwidth, screenheight);
                statustext = "<font align=\"right\">";
                foreach (string key in mycrucible.Storage.Keys)
                {
                    statustext += mycrucible.Storage[key] + "\n";
                }
                statustext += "</font>";
                SingleComposer.AddRichtext(statustext, CairoFont.WhiteDetailText(), screenBounds);
            }

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
            
            string statustext = "";
            statustext += "<font size=\"24\" align=\"center\" >";
            statustext += "CRUCIBLE STATUS\n\n";
            if (!mycrucible.IsOn)
            {
                statustext += "<font color=#ffff00>POWER OFF</font>";
            }
            else
            {
                statustext += "Power\n" + (int)(mycrucible.CapacitorPercentage * 100) + "%";
            }
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
            SingleComposer 
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
            int lineheight = 50;
            ElementBounds textBounds = ElementBounds.Fixed(column1start, sectiony, columnwidth*2, titleheight);
            string text = "<font size=\"24\" align=\"left\" >";
            text += "RECIPES(INGOTS)";
            text += "</font>";
            SingleComposer.AddRichtext(text, CairoFont.WhiteDetailText(), textBounds); //complete adding the title bar

            //FIRST COLUMN
            double yorigin = sectiony + titleheight * 1.25;
            double currentcolumnstart = column1start;
            double column1width = columnwidth * 2;
            double ytrack = yorigin;
            double textoffset = -11;
            double buttonheight = lineheight - 10;
            foreach (string key in mycrucible.Recipes.Keys)
            {
                textBounds = ElementBounds.Fixed(currentcolumnstart, ytrack-textoffset, column1width, lineheight);
                text = "<font size=\"24\" align=\"left\" >"+key.ToUpper() + "</font>";
                SingleComposer.AddRichtext(text, CairoFont.WhiteDetailText(), textBounds);
                ytrack += lineheight;
            }
       
            //THEN DRAW METAL QUANTITIES IN SECOND COLUMN
            ytrack = yorigin;
            currentcolumnstart += columnpad + column1width;
            foreach (string key in mycrucible.Recipes.Keys)
            {      
                textBounds = ElementBounds.Fixed(currentcolumnstart, ytrack-textoffset, columnwidth, lineheight);
                text = "<font size=\"24\" align=\"right\">"+mycrucible.Recipes[key] + "</font>";
                SingleComposer.AddRichtext(text, CairoFont.WhiteDetailText(), textBounds);
                ytrack += lineheight;
            }

            // DRAW MAKE BUTTONS
            currentcolumnstart += columnpad + columnwidth;
            ytrack = yorigin;
            foreach (string key in mycrucible.recipes.Keys)
            {
                textBounds = ElementBounds.Fixed(currentcolumnstart, ytrack, columnwidth, buttonheight);
                SingleComposer.AddButton("MAKE", ()=>onMetalSelect(key,1), textBounds, EnumButtonStyle.Normal, EnumTextOrientation.Center);
                ytrack += lineheight;
            }

            
            
        }
        

        private void powerswitch(Context ctx, ImageSurface surface, ElementBounds currentBounds)
        {
            ctx.Rectangle(0, 0, currentBounds.InnerWidth, currentBounds.InnerHeight);
            CompositeTexture tex = new CompositeTexture(new AssetLocation("game:block/panel elements.png"));
            Matrix m = ctx.Matrix;
            m.Scale(GuiElement.scaled(GEDrawTexture.scalefactor), GuiElement.scaled(GEDrawTexture.scalefactor));
            ctx.Matrix = m;


            AssetLocation loc = tex.Base.Clone().WithPathAppendixOnce(".png");

            GuiElement.fillWithPattern(capi, ctx, loc.Path, true, false);

            ctx.Restore();
            ctx.Save();
        }

        string activepaneltexture = "electrical panel.png";

        private void testdraw(Context ctx, ImageSurface surface, ElementBounds currentBounds)
        {
            
            ctx.Rectangle(0, 0, currentBounds.InnerWidth, currentBounds.InnerHeight);
            //CompositeTexture tex = liquidSlot.Itemstack.Collectible.Attributes?["waterTightContainerProps"]?["texture"]?.AsObject<CompositeTexture>(null, liquidSlot.Itemstack.Collectible.Code.Domain);
            CompositeTexture tex = new CompositeTexture(new AssetLocation("game:block/"+activepaneltexture));
            
            if (tex != null)
            {
                ctx.Save();
                Matrix m = ctx.Matrix;
                m.Scale(GuiElement.scaled(GEDrawTexture.scalefactor), GuiElement.scaled(GEDrawTexture.scalefactor));
                ctx.Matrix = m;
                

                AssetLocation loc = tex.Base.Clone().WithPathAppendixOnce(".png");

                GuiElement.fillWithPattern(capi, ctx, loc.Path, true, false);

                ctx.Restore();
            }
        }
        private bool onMetalSelect(string key, int qty)
        {
            mycrucible.SetOrder(key, qty, true);
            SetupStatusScreen();
            SetupInventoryScreen();
            SetupProductionScreen();
            SingleComposer.Compose();
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
            
            return true;
        }
        public bool onTogglePower()
        {
            mycrucible.ButtonTogglePower();
            SetupStatusScreen();
            SingleComposer.Compose();
            return true;
        }
        public void TogglePower(bool onoff)
        {
            mycrucible.ButtonTogglePower();
            SetupStatusScreen();
            SingleComposer.Compose();
        }
        public override bool TryClose()
        {
            opened = false;
            return base.TryClose();
        }
    }
}
