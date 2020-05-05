using System;
using System.Collections.Generic;
using System.Text;

namespace SharpDb.Models
{
    public class PredicateStep
    {
        public List<string> Predicates { get; set; }
        public List<string> PredicateTrailer { get; set; }
        public bool HasPredicates { get; set; }

    }
}
