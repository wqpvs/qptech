using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace questbook.src
{
    class Quest
    {
        public string questID;
        public string questName;
        public string questDescription;
        public string questTexture;
        public bool hidden;
        public bool locked;
        public bool completed;
        public List<QuestRequirement> questRequirements;
        public List<QuestReward> questRewards;
        public List<Quest> unlockedQuests;

        public Quest()
        {

        }
    }
}
