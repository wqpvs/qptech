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
    interface IQuestBookPage
    {
        string ID();
        string Name();
        string Description();
        List<IQuest> Quests();
        bool Unlocked();
        void ShowPage();
        void HidePage();

    }
}
