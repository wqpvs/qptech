using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace questbook.src
{
    interface IQuestBook
    {
        string ID();
        string Name();
        string Description();
        void OpenBook();
        void CloseBook();

        List<IQuestBookPage>Pages();

    }
}
