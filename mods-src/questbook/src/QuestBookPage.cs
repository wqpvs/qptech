using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace questbook.src
{
    /// <summary>
    /// A page from a questbook, including its rendering information
    /// </summary>
    interface QuestBookPage
    {
        string ID();
        string Name();
        string Description();
        List<Quest> Quests();
        bool Unlocked();
        void ShowPage();
        void HidePage();

    }
}
