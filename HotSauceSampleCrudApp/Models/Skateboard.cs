using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace HotSauceSampleCrudApp.Models
{
    public class Skateboard
    {
        public int SkateboardId { get; set; }
        [StringLength(50)]
        public string Brand { get; set; }
        public decimal Price { get; set; }
        public DateTime DateListed { get; set; }
        public bool Deleted { get; set; }
    }
}
