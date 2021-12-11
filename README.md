## About HotSauceDB
HotsauceDB is a file based database written in C#/.NET CORE. It is a hobby project and is not intended for production use. 

HotsauceDB implements its own SQL dialect, which is not ANSI compliant.

Its goal is to provide the developer a file based database and ORM with the smallest amount of configuration possible.

## Getting Started
To get started, simply install the HotSauceDbOrm nuget package. Its only dependency is the HotSauceDb nuget package.

Below is a small console app using HotSauceDB to create, insert, and update records.
```
using HotSauceDbOrm;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace TestHotSauce
{
    class Book
    {
        //adding a property with pattern
        //<class_name>id will designate
        //property as an auto incrementing field
        public int BookId { get; set; }
        [StringLength(50)]
        public string Name { get; set; }
        public DateTime ReleaseDate { get; set; }
        public int NumberOfPages { get; set; }
        public decimal Price { get; set; }
        public bool IsPublicDomain { get; set; }
        [StringLength(50)]
        public string Author { get; set; }

    }

    class Program
    {
        static void Main(string[] args)
        {
            Executor.GetInstance().DropDatabaseIfExists();
            var executor = Executor.GetInstance();

            //creating a table is an idempotent operation
            executor.CreateTable<Book>();

            executor.Insert(new Book
            {
                Name = "One Hundred Years of Solitude",
                Author = "GGM",
                IsPublicDomain = false,
                NumberOfPages = 150,
                Price = 15.99m,
                ReleaseDate = new DateTime(1967, 1, 1)
            });

            executor.Insert(new Book
            {
                Name = "Slaghterhouse Five",
                Author = "Vonnegut",
                IsPublicDomain = false,
                NumberOfPages = 150,
                Price = 15.99m,
                ReleaseDate = new DateTime(1969, 3, 31)
            });

            List<Book> books = executor.Read<Book>("select * from Book");

            Book book = books.First();

            book.NumberOfPages = 400;

            executor.Update<Book>(book);

            List<Book> modifiedBooks = executor.Read<Book>("select * from Book where Author = 'GGM'");
        }
    }
}

```

### ACID Compliance
- Atomicity - X
- Consistency - HotSauceDB doesn't really have rules, so...I guess?
- Isolation - 	&#10004;
- Durability - &#10004;

## Getting Started

#### The Basics

Complex objects are not supported as properties. The following data types are supported:
- Boolean
- Char
- Decimal
- Int32
- Int64
- String
- DateTime

*String properties must contain an attribute 
indicating the maximum length of the string

```
    public class House
    {
        public int HouseID { get; set; }
        public int NumBedrooms { get; set; }
        public decimal Price { get; set; }
        public bool IsListed { get; set; }
        public DateTime DateListed { get; set; }

        [StringLength(50)]
        public string Address { get; set; }
    }
```

#### Create a Table
```            
Executor executor = Executor.GetInstance();

executor.CreateTable<House>();
```

#### Insert Some Data
```
	var h = new House   
	{
		NumBedrooms = 2,
		NumBath = 1,
		Address = "7450 Calm Lane",
		Price = 125000,
		IsListed = false
	};
	
	executor.Insert(h);
```


#### Read

```
string query = "select * FROM house where address = '7450 Calm Lane'";
List<House> houses = executor.Read<House>(query);
```
#### Update
In order to perform update operations, the entity must have a property with type Int32 or Int64 with naming convention EntityNameId. So, if your entity is named House, the name would be "HouseId".

```
House house = new House();
executor.Insert(h);

house.Prce = 150000;
executor.Update(h);
```
## Supported Operators
- =
- !=
- <
- <=
- \>
- \>=
- IN
- OR
- AND

## Examples of Supported Queries

Only inserts, reads, and updates are supported at this time. 

The following read operations are all valid examples for HotSauceDB:

```
select * 
from house
```

```
select Address, Price 
from house
```

```
select * 
from house 
where address = '450 Adams St'
```

```
select * 
from houses 
 where address = '450 Adams St'
  AND price > 315000
 ```

```
select * 
from house 
where price = (select price from house where address = '450 Adams St' )
```

```
select * 
from house
where address != '98765 ABC str'
AND Price > 269000
order by price
```

```
select Price, Max(NumBedRooms), Min(NumBathrooms)
from house
GROUP BY PRICE
```


```
select * 
from house 
where price = (select price from house where address = '450 Adams St' )
and NumBedrooms = (select NumBedrooms from house where address = '123 ABC St')
OR NumBath > 5
```

```
select * 
from house 
where price in (150, 170, 180)
```


