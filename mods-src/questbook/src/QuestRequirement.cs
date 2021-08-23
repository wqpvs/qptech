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
    interface IQuestRequirement
    {
        string ID();
        string Name();
        string Description();

        bool IsComplete();

        
    }
}
