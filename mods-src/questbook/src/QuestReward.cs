using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace questbook.src
{
    class QuestReward
    {
        public string rewardtext;
        public string code;
        public int quantity;
        public bool hidden = false;
        public bool claimed = false;
        public QuestReward()
        {

        }
        /// <summary>
        /// Quest rewards (quests can have multiple of these). Can just be a message, or can give a quantity
        /// of blocks or items
        /// </summary>
        /// <param name="rewardtext">Message Award</param>
        /// <param name="code">Block or Item code (incl. domain and path, can be black)</param>
        /// <param name="quantity">Amount of award (can be 0)</param>
        /// <param name="hidden">Whether to show this reward in the quest book</param>
        public QuestReward(string rewardtext,string code, int quantity, bool hidden)
        {
            this.rewardtext = rewardtext;
            this.code = code;
            this.quantity = quantity;
            this.hidden = hidden;
        }
        
    }
}
