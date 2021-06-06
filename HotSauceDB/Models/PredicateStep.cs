using System.Collections.Generic;

namespace HotSauceDb.Models
{
    public class PredicateStep
    {
        public List<string> Predicates { get; set; }
        public List<string> PredicateTrailer { get; set; }
        public bool HasPredicates { get; set; }
        public int? OperatorIndex { get; set; }
        public List<string> QueryParts { get; set; }

    }
}
