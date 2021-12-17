using System.ComponentModel.DataAnnotations;

namespace HotSauceIntegrationTests.TestModels
{
    public class House
    {
        public int HouseId { get; set; }
        public int NumBedrooms { get; set; }
        public int NumBath { get; set; }
        public decimal Price { get; set; }
        public bool IsListed { get; set; }

        [StringLength(50)]
        public string Address { get; set; }
    }
}
