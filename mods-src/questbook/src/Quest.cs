using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace questbook.src
{
    interface Quest
    {
        string ID();
        string Name();
        string Description();
        bool IsComplete();
        bool RewardsClaimed();
        List<QuestReward> Rewards();
        List<QuestRequirement> Requirements();

    }
}
