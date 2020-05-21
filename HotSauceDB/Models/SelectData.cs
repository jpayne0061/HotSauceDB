using System;
using System.Collections.Generic;
using System.Text;

namespace HotSauceDB.Models
{
    public class SelectData
    {
        public List<List<IComparable>> Rows { get; set; }

        public List<long> RowLocations { get; set; }
    }
}
