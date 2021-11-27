using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qptech.src.networks
{
    interface IProcessingSupplier
    {
        bool RequestProcessing(string process, double amount);
        double RequestProcessing(string process);
        bool CheckProcessing(string process);
    }
}
