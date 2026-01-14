using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.QBCHRequisitsService;

namespace Application_lib
{
    public interface IQBCHRequisitsService
    {
        public List<QBCHRequisite> GetBureaList();
    }
}
