using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace questbook.src
{
    interface IQuest
    {
        string ID
        {
            get;
        }
        string Name
        {
            get;
        }
        string Description { get; }
        bool IsComplete();
        bool RewardsClaimed();
        List<IQuestReward> Rewards();
        List<IQuestRequirement> Requirements();

    }
}
