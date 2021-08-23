using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace questbook.src
{

/// <summary>
/// Requirement to finish a given quest
/// </summary>
    class QuestRequirement
    {
        public string code;
        public int quantity;
        public bool isRemovedOnClaim;
        public bool completed = false;
        public QuestRequirement()
        {

        }

        /// <summary>
        /// Returns a new QuestRequirement
        /// </summary>
        /// <param name="code">Item or Block code with full path</param>
        /// <param name="quantity">Amount of that item code that must be present</param>
        /// <param name="isRemovedOnClaim">Whether this item is removed when the quest is claimed</param>
        public QuestRequirement (string code, int quantity, bool isRemovedOnClaim)
        {
            this.code = code;
            this.quantity = quantity;
            this.isRemovedOnClaim = isRemovedOnClaim;
            
        }

        
    }
}
