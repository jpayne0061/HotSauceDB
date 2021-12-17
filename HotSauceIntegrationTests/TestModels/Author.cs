using HotSauceDB.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HotSauceIntegrationTests.TestModels
{
    public class Author
    {
        public int AuthorId { get; set; }
        [StringLength(50)]
        public string Name { get; set; }
        public DateTime BirthDate { get; set; }
        [RelatedEntity("Book")]
        public List<Book> Books { get; set; }
    }
}
