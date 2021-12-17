using HotSauceDB.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace HotSauceIntegrationTests.TestModels
{
    public class Book
    {
        public int BookId { get; set; }
        [StringLength(50)]
        public string Name { get; set; }
        public DateTime ReleaseDate { get; set; }
        public int NumberOfPages { get; set; }
        public decimal Price { get; set; }
        public bool IsPublicDomain { get; set; }
        public int AuthorId { get; set; }
        [RelatedEntity("Author")]
        public Author Author { get; set; }
    }
}
