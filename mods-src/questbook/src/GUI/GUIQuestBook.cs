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
        IQuestBook myBook;
        int currentPage = 0;
        public GUIQuestBook(string dialogTitle, ICoreClientAPI capi, IQuestBook book) : base(dialogTitle, capi)
        {
            myBook = book;
            api = capi;
        }

        public virtual void SetupDialog(string texturename,double basetexturesize)
        {
            
            ElementBounds dialogBounds = ElementBounds.Fixed(0, 0, basetexturesize, basetexturesize);
            SingleComposer = capi.Gui.CreateCompo("Questbook1", dialogBounds);
            ///
            /// render code goes here
            ///
            
            ElementBounds qbBounds = ElementBounds.Fixed(0, 0,basetexturesize,basetexturesize);
            GEDrawTexture gdt = new GEDrawTexture(capi, qbBounds, texturename);
            SingleComposer.AddDynamicCustomDraw(qbBounds, gdt.OnDraw);
            SingleComposer.BeginChildElements(qbBounds);
            if (myBook != null&&myBook.Pages()!=null&&myBook.Pages().Count>currentPage)
            {
                QuestBookPageRenderer renderpage= new QuestBookPageRenderer(capi, qbBounds, myBook.Pages()[currentPage],SingleComposer);
            }
            SingleComposer.EndChildElements();
            SingleComposer.Compose();

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
