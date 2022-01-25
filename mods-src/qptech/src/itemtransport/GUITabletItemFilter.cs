using System;
using System.Linq;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace qptech.src.itemtransport
{
    public class GUITabletItemFilter : GuiDialog
    {
        public override string ToggleKeyCombinationCode => "annoyingtextgui";

        public GUITabletItemFilter(ICoreClientAPI capi) : base(capi)
        {
            SetupDialog();
        }

        public void SetupDialog()
        {
            // Auto-sized dialog at the center of the screen
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            // Just a simple 300x300 pixel box
            ElementBounds titleTextBounds = ElementBounds.Fixed(80, 13, 137, 9);
            ElementBounds filterTextBounds = ElementBounds.Fixed(16, 28, 268, 34);
            ElementBounds copyModeButtonBounds = ElementBounds.Fixed(16, 66, 86, 27);
            ElementBounds applyModeButtonBounds = ElementBounds.Fixed(106, 66, 86, 27);
            ElementBounds clearModeButtonBounds = ElementBounds.Fixed(197, 66, 86, 27);
            // Background boundaries. Again, just make it fit it's child elements, then add the text as a child element
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(titleTextBounds,filterTextBounds,copyModeButtonBounds,applyModeButtonBounds,clearModeButtonBounds);

            string itemfiltertext = "NO FILTER";
            if (TabletTool.itemfilter != null) { itemfiltertext = TabletTool.itemfilter.GetFilterDescription(); }

            // Lastly, create the dialog
            SingleComposer = capi.Gui.CreateCompo("myAwesomeDialog", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar("FluxCompOS - Item Filter Mode", OnTitleBarCloseClicked)
                .AddStaticText(itemfiltertext, CairoFont.WhiteDetailText(), titleTextBounds)
                .AddButton("COPY",OnCopyModeButton,copyModeButtonBounds)
                .AddButton("APPLY",OnApplyModeButton,applyModeButtonBounds)
                .AddButton("CLEAR",OnClearModeButton,clearModeButtonBounds)
                .Compose()
            ;
        }

        bool OnCopyModeButton()
        {
            TabletTool.tabletMode = TabletTool.enTabletMode.CopyFilter;
            TryClose();
            return true;
        }

        bool OnApplyModeButton()
        {
            TabletTool.tabletMode = TabletTool.enTabletMode.SetFilter;
            TryClose();
            return true;
        }

        bool OnClearModeButton()
        {
            TabletTool.tabletMode = TabletTool.enTabletMode.ClearFilter;
            TryClose();
            return true;
        }
        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }
    }

    
}
