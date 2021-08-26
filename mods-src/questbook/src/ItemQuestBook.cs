using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using questbook.src.GUI;
using questbook.src.SampleQuest;

namespace questbook.src
{
    class ItemQuestBook:Item
    {
        GUIQuestBook myGUI;
        public string basetexture= "gui/questbook.png";
        public double basetexturesize=512;
        
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            if (!(api is ICoreClientAPI)) { return; }
            SimpleQuestBook book = new SimpleQuestBook();
            book.id = "test";
            book.description = "magical test book";
            book.name = "Test Book";
            SimpleQuestBookPage bookpage = new SimpleQuestBookPage("test1","Page One","Page the First");
            SimpleQuest quest = new SimpleQuest("quest1", "Testor", "Do a Thing", "gui/bubblewindow.png");
            bookpage.AddQuest(quest);
            SimpleQuest quest2 = new SimpleQuest("quest2", "Testy", "Do another Thing", "gui/bubblewindow.png");
            bookpage.AddQuest(quest2);
            book.AddPage(bookpage);


            if (myGUI == null) { myGUI = new GUIQuestBook("Questbook" + byEntity.EntityId, api as ICoreClientAPI,book); }
            myGUI.TryOpen();
            myGUI.SetupDialog(basetexture,basetexturesize);
        }
    }
}
