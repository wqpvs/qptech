using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace questbook.src.SampleQuest
{
    class SimpleQuest : IQuest
    {
        public string id ;

        public string name ;

        public string description ;

        public string texturename;
        public string completedtexturename;
        public string ID => id;
        public bool iscomplete;
        string IQuest.Name => name;

        string IQuest.Description => description;
        public SimpleQuest()
        {

        }

        public SimpleQuest(string id,string name,string description, string texturename, string completedtexturename)
        {
            this.id = id;
            this.name = name;
            this.description = description;
            this.texturename = texturename;
            this.completedtexturename = completedtexturename;
            this.iscomplete = false;

        }
        public bool IsComplete()
        {
            return iscomplete;
        }

        public List<IQuestRequirement> Requirements()
        {
            return null;
        }

        public List<IQuestReward> Rewards()
        {
            return null;
        }

        public bool RewardsClaimed()
        {
            return false;
        }

        public void CheckComplete()
        {
            iscomplete = true;
        }
    }
}
