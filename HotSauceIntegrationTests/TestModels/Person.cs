using System.ComponentModel.DataAnnotations;

namespace HotSauceIntegrationTests.TestModels
{
    public class Person
    {
        public int Age { get; set; }

        [StringLength(50)]
        public string Name { get; set; }
        public decimal Height { get; set; }

    }
}
