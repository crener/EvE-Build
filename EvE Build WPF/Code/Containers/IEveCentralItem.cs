using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvE_Build_WPF.Code.Containers
{
    interface IEveCentralItem
    {
        void setBuyCost(int currentStation, decimal cost);
        void setSellCost(int currentStation, decimal cost);
    }
}
