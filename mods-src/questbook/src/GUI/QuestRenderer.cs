using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using questbook.src.SampleQuest;

namespace questbook.src.GUI
{
    class QuestRenderer
    {
        ICoreClientAPI api;
        IQuest myQuest;
        public QuestRenderer(ICoreClientAPI capi, ElementBounds bounds, GuiComposer SingleComposer, IQuest quest)
        {
            myQuest = quest;
            if (myQuest is SimpleQuest)
            {
                SimpleQuest sq = myQuest as SimpleQuest;
                GEDrawTexture gdt = new GEDrawTexture(capi, bounds, sq.texturename);
                SingleComposer.AddDynamicCustomDraw(bounds, gdt.OnDraw);
            }
        }
    }
}
