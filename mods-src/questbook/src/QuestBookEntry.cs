using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace questbook.src
{
    /// <summary>
    /// Entry for a page in a quest book, including the location of the quest in the book
    /// TODO: Should also allow for non-quest entries, text, buttons, etc?
    /// </summary>
    class QuestBookEntry
    {
        
        public Quest quest;
        public double x;
        public double y;
        public QuestBookEntry()
        {

        }
    }
}
