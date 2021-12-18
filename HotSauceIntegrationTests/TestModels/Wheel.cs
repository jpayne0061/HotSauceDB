using System.ComponentModel.DataAnnotations;

namespace HotSauceIntegrationTests.TestModels
{
    public class Wheel
    {
        public int WheelId { get; set; }
        [StringLength(20)]
        public string Name { get; set; }
        public int SkateboardId { get; set; }
    }
}
