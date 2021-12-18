using HotSauceDB.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HotSauceIntegrationTests.TestModels
{
    public class Order
    {
        public long OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        [StringLength(20)]
        public string Address { get; set; }
        [RelatedEntity("Skateboard")]
        public List<Skateboard> Items { get; set; }
    }
}
