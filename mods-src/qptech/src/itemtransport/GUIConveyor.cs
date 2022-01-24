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
    class GUIConveyor:GuiDialogBlockEntity
    {
        ICoreClientAPI api;
        Conveyor conveyor;
        ItemFilter itemfilter;
        public GUIConveyor(string dialogTitle, BlockPos blockEntityPos, ICoreClientAPI capi) : base(dialogTitle,blockEntityPos,capi)
        {
            api = capi;
        }
        public void SetupDialog(Conveyor conveyor)
        {
            this.conveyor = conveyor;
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
            ElementBounds allowTextBounds = ElementBounds.Fixed(21, 133, 325, 30);
            ElementBounds allowTextInputBounds = ElementBounds.Fixed(21, 171, 551, 37);            
            ElementBounds mustMatchAllBoxBounds = ElementBounds.Fixed(408,133,164,25);
            ElementBounds blockTextBounds = ElementBounds.Fixed(21, 223, 254, 30);
            ElementBounds blockTextInputBounds = ElementBounds.Fixed(21, 262, 551, 37);
            ElementBounds applyButtonBounds = ElementBounds.Fixed(21, 531, 151, 39);
            
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);

            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(
                allowTextBounds,allowTextInputBounds,mustMatchAllBoxBounds,
                applyButtonBounds,
                blockTextBounds,blockTextInputBounds);
            string guicomponame = conveyor.Pos.ToString()+" Conveyor";
            if (conveyor.itemfilter == null)
            {
                itemfilter = new ItemFilter();
            }
            else
            {
                itemfilter = conveyor.itemfilter.Copy();
            }


            SingleComposer = capi.Gui.CreateCompo(guicomponame, dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar("Item Filter Setup", OnTitleBarCloseClicked)
                .AddRichtext("Only allow objects with", CairoFont.WhiteDetailText(), allowTextBounds)
                .AddTextInput(allowTextInputBounds, OnChangeItemFilterInput, CairoFont.WhiteDetailText(), "allow")
                .AddToggleButton("Match All",CairoFont.WhiteDetailText(),OnMatchAllToggle,mustMatchAllBoxBounds,"mustmatchall")
                .AddRichtext("Block objects with",CairoFont.WhiteDetailText(),blockTextBounds)
                .AddTextInput(blockTextInputBounds,OnChangeBlockFilterInput,CairoFont.WhiteDetailText(),"block")
                .AddButton("Apply", OnApplyButton, applyButtonBounds);

            ;
            SingleComposer.GetToggleButton("mustmatchall").SetValue(itemfilter.mustmatchall);
            SingleComposer.GetTextInput("allow").SetValue(itemfilter.allowonlymatch);
            SingleComposer.GetTextInput("block").SetValue(itemfilter.blockonlymatch);
            SingleComposer.Compose();

        }

        

        public void OnChangeItemFilterInput(string newinput)
        {
            itemfilter.allowonlymatch = newinput;
        }

        public void OnChangeBlockFilterInput(string newinput)
        {
            itemfilter.blockonlymatch = newinput;
        }

        public void OnMatchAllToggle(bool newvalue)
        {
            itemfilter.mustmatchall = newvalue;
        }

        public override bool TryOpen()
        {
            
            return base.TryOpen();
        }
        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }
        bool OnApplyButton()
        {
            conveyor.OnNewFilter(itemfilter);
            TryClose();
            return true;
        }

    }
}
