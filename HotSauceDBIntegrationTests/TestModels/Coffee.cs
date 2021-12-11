using System;
using System.ComponentModel.DataAnnotations;

namespace HotSauceDBIntegrationTests.TestModels
{
    public class Coffee
    {
        [StringLength(5)]
        public string Name { get; set; }
        public decimal Price { get; set; }
        [StringLength(10)]
        public string NewProperty { get; set; }
        public DateTime SellByDate { get; set; }
        public DateTime NewDate { get; set; }
    }
}


namespace HotSauceDBIntegrationTests.TestNamespace
{
    public class Coffee
    {
        [StringLength(5)]
        public string Name { get; set; }
        public decimal Price { get; set; }
        [StringLength(10)]
        public string NewProperty { get; set; }
        public DateTime SellByDate { get; set; }
        public DateTime NewDate { get; set; }
        public int newInt { get; set; }
        public int Ounces { get; set; }
        public bool IsTasty { get; set; }
        public bool IsBrown { get; set; }
        public char Letter { get; set; }
    }
}
