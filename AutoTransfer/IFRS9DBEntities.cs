using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTransfer
{
    public class IFRS9DBEntities : IFRS9Entities
    {
        public IFRS9DBEntities()
        {
            ((IObjectContextAdapter)this).ObjectContext.CommandTimeout = 300;
        }
    }
}
