using System;
using System.ComponentModel.DataAnnotations;

namespace HotSauceDBIntegrationTests.TestModels
{
    public class Coffee
    {
        public int Ounces { get; set; }
        [StringLength(5)]
        public string Name { get; set; }
        public decimal Price { get; set; }
        public DateTime SellByDate { get; set; }
        [StringLength(50)]
        public string NewProperty { get; set; }
        public DateTime NewDate { get; set; }
        public int newInt { get; set; }
    }
}
