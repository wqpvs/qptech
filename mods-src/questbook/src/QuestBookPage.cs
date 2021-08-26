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
        string ID { get; }
        string Name { get; }
        string Description
        {
            get;
        }
        List<IQuest> Quests();

        void AddQuest(IQuest quest);
        bool Unlocked();
        void ShowPage();
        void HidePage();

    }
}
