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

        double sectionx = 0;
        double sectiony = 40;
        
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

            SingleComposer.Compose();
            
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
            SingleComposer.AddDynamicCustomDraw(dialogBounds, Testdraw, "testbar");
            double switchx=5;
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
            ElementBounds switchBounds = ElementBounds.Fixed(switchx, switchy, 64, 24);
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
                screenx = sectionx + screenxoffest + 36;
                screeny = sectiony + 55;
                screenBounds = ElementBounds.Fixed(screenx, screeny, screenwidth, screenheight);
                statustext = "<font align=\"left\" >";
                foreach (string key in mycrucible.Storage.Keys)
                {
                    statustext += key + "\n";
                }
                statustext += "</font>";
                SingleComposer.AddRichtext(statustext, CairoFont.WhiteDetailText(), screenBounds);
                screenx = sectionx + 160 + screenwidth / 2+20;
                screenBounds = ElementBounds.Fixed(screenx, screeny, screenwidth, screenheight);
                statustext = "<font align=\"right\">";
                foreach (string key in mycrucible.Storage.Keys)
                {
                    statustext += mycrucible.Storage[key] + "\n";
                }
                statustext += "</font>";
                SingleComposer.AddRichtext(statustext, CairoFont.WhiteDetailText(), screenBounds);

                //Now let's try and draw the production panel
                double psectionx = 0;
                double psectiony = 256+32;
                dialogBounds = ElementBounds.Fixed(psectionx, psectiony, 512, 256);
                gdt = new GEDrawTexture(capi, dialogBounds, "blankpanel.png");
                SingleComposer.AddDynamicCustomDraw(dialogBounds, gdt.OnDraw);

                //drawing the production mode button
                double buttonstartx = 26;
                double buttonstarty = 24;
                double buttonwidth = 32;
                double buttonheight = 32;
                double labelxpad = 5;
                double labelypad = 8;
                double maxVertSize = buttonheight * 6;
                string buttontexture = "dialswitchup.png";
                dialogBounds = ElementBounds.Fixed(psectionx+buttonstartx, psectiony+buttonstarty, buttonwidth, buttonheight);
                if (mycrucible.Mode == enProductionMode.REPEAT) { buttontexture = "dialswitchright.png"; }
                //TODO why don't buttons render - possibly we can just cheat here and draw our own buttons?
                
                //REPEAT MODE
                SingleComposer.AddButton("", onToggleMode, dialogBounds);
                gdt = new GEDrawTexture(capi, dialogBounds, buttontexture);
                SingleComposer.AddDynamicCustomDraw(dialogBounds, gdt.OnDraw);
                dialogBounds = ElementBounds.Fixed(psectionx + buttonstartx + buttonwidth + labelxpad, psectiony+labelypad + buttonstarty, 200, buttonheight);
                SingleComposer.AddRichtext("REPEAT MODE", CairoFont.WhiteDetailText(), dialogBounds);
                buttonstarty += buttonheight;
                
                //EMERGENCY STOP
                dialogBounds = ElementBounds.Fixed(psectionx + buttonstartx, psectiony + buttonstarty, buttonwidth, buttonheight);
                SingleComposer.AddButton("", onHaltProduction, dialogBounds);
                gdt = new GEDrawTexture(capi, dialogBounds, "stopbutton.png");
                SingleComposer.AddDynamicCustomDraw(dialogBounds, gdt.OnDraw);
                dialogBounds = ElementBounds.Fixed(psectionx + buttonstartx + buttonwidth + labelxpad, psectiony + labelypad + buttonstarty, 200, buttonheight);
                SingleComposer.AddRichtext("HALT PRODUCTION", CairoFont.WhiteDetailText(), dialogBounds);
                buttonstarty += buttonheight;

                if (mycrucible.recipes == null || mycrucible.recipes.Count == 0) { return; }
                //PRODUCTION BUTTONS
                foreach (string key in mycrucible.recipes.Keys)
                {
                    string switchtexture = "dialswitchup.png";
                    if (mycrucible.Making == key) { switchtexture = "dialswitchright.png"; }
                    dialogBounds = ElementBounds.Fixed(psectionx + buttonstartx, psectiony + buttonstarty, buttonwidth, buttonheight);
                    SingleComposer.AddButton("", ()=>onProductionButton(key), dialogBounds);
                    gdt = new GEDrawTexture(capi, dialogBounds, switchtexture);
                    SingleComposer.AddDynamicCustomDraw(dialogBounds, gdt.OnDraw);
                    dialogBounds = ElementBounds.Fixed(psectionx + buttonstartx + buttonwidth + labelxpad, psectiony + labelypad + buttonstarty, 200, buttonheight);
                    SingleComposer.AddRichtext(key, CairoFont.WhiteDetailText(), dialogBounds);
                    buttonstarty += buttonheight;
                    if (buttonstarty > maxVertSize) { buttonstarty = 24;buttonstartx += 256; }
                }
                
            }

        }
       
        private void powerswitch(Context ctx, ImageSurface surface, ElementBounds currentBounds)
        {
            ctx.Rectangle(0, 0, currentBounds.InnerWidth, currentBounds.InnerHeight);
            CompositeTexture tex = new CompositeTexture(new AssetLocation("game:block/redhandle.png"));
            Matrix m = ctx.Matrix;
            m.Scale(GuiElement.scaled(GEDrawTexture.scalefactor), GuiElement.scaled(GEDrawTexture.scalefactor));
            ctx.Matrix = m;
            AssetLocation loc = tex.Base.Clone().WithPathAppendixOnce(".png");
            GuiElement.fillWithPattern(capi, ctx, loc.Path, true, false);
            ctx.Restore();
            ctx.Save();
        }

        string activepaneltexture = "electrical panel.png";

        private void Testdraw(Context ctx, ImageSurface surface, ElementBounds currentBounds)
        {
            
            ctx.Rectangle(0, 0, currentBounds.InnerWidth, currentBounds.InnerHeight);
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
            mycrucible.HaltButton();
            TryClose();
            return true;
        }
        public bool onTogglePower()
        {
            mycrucible.ButtonTogglePower();
            
            SingleComposer.Compose();
            return true;
        }
        public bool onToggleMode()
        {
            mycrucible.ButtonToggleMode();
            
            SingleComposer.Compose();
            return true;
        }
        public void TogglePower(bool onoff)
        {
            mycrucible.ButtonTogglePower();
            
            SingleComposer.Compose();
        }

        public override bool TryClose()
        {
            opened = false;
            return base.TryClose();
        }

        public bool onProductionButton(string material)
        {
            mycrucible.SetOrder(material, 1, true);
            TryClose();
            return true;
        }
    }
}
