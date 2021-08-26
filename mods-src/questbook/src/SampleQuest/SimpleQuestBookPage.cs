using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace questbook.src.SampleQuest
{
    class SimpleQuestBookPage : IQuestBookPage
    {
        string id;
        public string ID => id;

        string name;
        public string Name => name;

        string description;
        public string Description => description;
        List<IQuest> quests;

        public SimpleQuestBookPage()
        {

        }

        public SimpleQuestBookPage(string id, string name, string description)
        {
            this.id = id;
            this.name = name;
            this.description = description;
        }
        public void AddQuest(IQuest quest)
        {
            if (Quests().Where(p => p.ID == quest.ID).Count() > 0) { return; }
            Quests().Add(quest);
        }

        public void HidePage()
        {
            
        }

        public List<IQuest> Quests()
        {
            if (quests == null) { quests = new List<IQuest>(); }
            return quests;
        }

        public void ShowPage()
        {
            
        }

        public bool Unlocked()
        {
            return true;
        }
    }
}
