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
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            ElementBounds applyButtonBounds = ElementBounds.Fixed(21, 531, 151, 39);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(allowTextBounds,allowTextInputBounds,applyButtonBounds);
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
                .AddDialogTitleBar("Machine Status", OnTitleBarCloseClicked)
                .AddRichtext("Only allow objects with", CairoFont.WhiteDetailText(), allowTextBounds)
                .AddTextInput(allowTextInputBounds, OnChangeItemFilterInput, CairoFont.WhiteDetailText(), itemfilter.allowonlymatch)
                .AddButton("Apply", OnApplyButton, applyButtonBounds);
            ;
            
            SingleComposer.Compose();

        }
        public void OnChangeItemFilterInput(string newinput)
        {
            itemfilter.allowonlymatch = newinput;
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
