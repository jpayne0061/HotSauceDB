using System;
using System.Collections.Generic;
using System.Text;

namespace SharpDb.Models
{
    public class InnerStatement
    {
        public string Statement { get; set; }
        public int StartIndexOfOpenParantheses { get; set; }
        public int EndIndexOfCloseParantheses { get; set; }
    }
}
