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
    class GUIAssemblerStatus:GuiDialogBlockEntity
    {
        ICoreClientAPI api;
        public GUIAssemblerStatus(string dialogTitle, BlockPos blockEntityPos, ICoreClientAPI capi) : base(dialogTitle,blockEntityPos,capi)
        {
            api = capi;
        }
        public void SetupDialog(BEEAssembler bea)
        {
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            // Just a simple 300x100 pixel box with 40 pixels top spacing for the title bar
            ElementBounds textBounds = ElementBounds.Fixed(0, 40, 600, 600);

            // Background boundaries. Again, just make it fit it's child elements, then add the text as a child element
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(textBounds);
            string guicomponame = bea.Pos.ToString()+"Assembler";
            string statustext = "";
            string alertred = "<font color=\"#ffbbaa\">";//<font <color=\"#ffdddd\">>";
            
            if (!bea.IsPowered)
            {
                statustext += alertred;
            }
            else if (bea.DeviceState == BEEBaseDevice.enDeviceState.MATERIALHOLD)
            {
                statustext += alertred;
            }
            else if (bea.DeviceState == BEEBaseDevice.enDeviceState.RUNNING)
            {
                statustext += "<font color=\"#aaff55\">";
            }
            else
            {
                statustext += "<font>";
            }
            statustext += "<strong>STATUS: " + bea.Status + "</strong></font><br><br>";
            statustext += "Making <strong>" + bea.FG.ToUpper() + "</strong><br><br>";
            statustext += "Requires <strong>" + bea.RM +"</strong>";
            if (bea.Materials.Length > 0&&bea.DeviceState==BEEBaseDevice.enDeviceState.MATERIALHOLD)
            {
                
                statustext += " of material ";
                statustext += "<font><strong>";
                
                for (int c=0;c<bea.Materials.Length;c++)
                {
                    if (c == bea.Materials.Length - 1) { statustext += ", or "; }
                    else if (c != 0) { statustext += ", "; }
                    
                    statustext += bea.Materials[c];
                    
                }
                statustext += ".</font></strong>";
            }
            SingleComposer = capi.Gui.CreateCompo(guicomponame, dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar("Metal Press Status", OnTitleBarCloseClicked)
                .AddRichtext(statustext, CairoFont.WhiteDetailText(), textBounds)
                
                .Compose()
            ;

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
