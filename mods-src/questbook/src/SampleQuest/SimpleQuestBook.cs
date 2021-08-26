using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace questbook.src.SampleQuest
{
    class SimpleQuestBook : IQuestBook
    {
        public string id;
        public string name;
        public string description;
        

        public string ID=>id ;

        public string Name=>name ;

        public string Description=>description ;
        List<IQuestBookPage> questbookpages;
        
        public void CloseBook()
        {
            
        }

        public void OpenBook()
        {
            
        }
        public void AddPage(IQuestBookPage newpage)
        {
            if (Pages().Where(p => p.ID == newpage.ID).Count() > 0) { return; }
            Pages().Add(newpage);
        }
        public List<IQuestBookPage> Pages()
        {
            if (questbookpages == null) { questbookpages = new List<IQuestBookPage>(); }
            return questbookpages;
        }
    }
}
