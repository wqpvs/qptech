using System;
using System.Linq;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace questbook.src.GUI
{
    class GUIQuestBook : GuiDialogGeneric
    {
        ICoreClientAPI api;
        public GUIQuestBook(string dialogTitle, ICoreClientAPI capi) : base(dialogTitle, capi)
        {
            api = capi;
        }

        public void SetupDialog()
        {
            
            ElementBounds dialogBounds = ElementBounds.Fixed(0, 0, 512, 512);
            SingleComposer = capi.Gui.CreateCompo("Questbook", dialogBounds);
            ///
            /// render code goes here
            /// 
            TestRender(dialogBounds);
            SingleComposer.Compose();

        }

        void TestRender(ElementBounds bounds)
        {
            GEDrawTexture testtexture = new GEDrawTexture(capi, bounds, "gui/questbook.png");
        }

        public override bool TryOpen()
        {

            return base.TryOpen();
        }
        public override bool TryClose()
        {
            return base.TryClose();
        }
    }
}
