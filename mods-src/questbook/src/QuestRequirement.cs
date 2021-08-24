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
        string ID { get; }
        string Name { get; }
        string Description
        {
            get;
        }

        bool IsComplete();

        
    }
}
