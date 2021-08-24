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
            if (myGUI == null) { myGUI = new GUIQuestBook("Questbook" + byEntity.EntityId, api as ICoreClientAPI); }
            myGUI.TryOpen();
            myGUI.SetupDialog(basetexture,basetexturesize);
        }
    }
}
