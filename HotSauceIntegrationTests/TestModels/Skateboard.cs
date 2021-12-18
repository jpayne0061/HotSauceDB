using HotSauceDB.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HotSauceIntegrationTests.TestModels
{
    public class Skateboard
    {
        [Identity]
        public int BoardId { get; set; }
        [StringLength(20)]
        public string Brand { get; set; }
        public decimal Price { get; set; }
        public DateTime DateCreated { get; set; }
        public bool IsColor { get; set; }
        public long OrderId { get; set; }
        [RelatedEntity(typeof(Wheel))]
        public List<Wheel> Wheels { get; set; }
    }
}
