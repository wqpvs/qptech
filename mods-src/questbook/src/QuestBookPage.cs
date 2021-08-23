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
    class QuestBookPage
    {
        public string pageID;
        public string pageName;
        public string backgroundTextureName;
        public List<QuestBookEntry> entries;

    }
}
