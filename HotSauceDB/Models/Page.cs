using System;
using System.Collections.Generic;
using System.Text;

namespace HotSauceDB.Models
{
    public class BackfillPage
    {
        public long StartAddress { get; set; }
        public long NextPagePointer { get; set; }
        public short NewRowCount { get; set; }
        public short OldRowCount { get; set; }
        public bool NewlyAllocatedPage { get; set; }
    }
}
