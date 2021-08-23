using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace questbook.src
{
    interface QuestReward
    {
        string RewardID();
        string RewardText();
        bool IsClaimed();
        bool TryClaim();

    }
}
