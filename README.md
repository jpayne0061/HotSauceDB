## What is HotSauceDB?

HotSauceDB is a database written in C#/.NET Core 2.1. It is a hobby project and is not intended 
for production use.

## Getting Started

HotSauceDB has a basic ORM. Here is how we would use it


###### Create a Model

Complex objects are not supported as properties. String proeprties must contain an attribute 
indicating the maximum length of the string

```
    public class House
    {
        public int NumBedrooms { get; set; }
        public decimal Price { get; set; }
        public bool IsListed { get; set; }
        public DateTime DateListed { get; set; }

        [StringLength(50)]
        public string Address { get; set; }
    }
```

###### Create a Table

Every operation will run through the executor object

```            
var executor = new Executor();

executor.CreateTable<House>();
```

###### Insert Some Data

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


###### Retrieve the Data

```
List<House> houses = executor.Read<House>("select * FROM house where address = '7450 Calm Lane'");
```

## What Can HotSauceDB do?

Curently, HotSauceDB supports the creation of tables with the following types:

- Boolean
- Char
- Decimal
- Int32
- Int64
- String
- DateTime

Only inserts and reads are supported at this time. HotsauceDB supports mutlithreaded 
applications.

The following read operations are all valid for HotSauceDB

`select * from house`

`select Address, Price from house`

`select * from houses where address = '450 Adams St'`

`select * 
from houses 
 where address = '450 Adams St'
  AND price > 315000`

`select * from house 
where price = (select price from house where address = '450 Adams St' )`

`select * from house
where address != '98765 ABC str'
AND Price > 269000
order by price`

`select Price, Max(NumBedRooms), Min(NumBathrooms)
from house
GROUP BY PRICE`


`select * from house 
where price = (select price from house where address = '450 Adams St' )
and NumBedrooms = (select NumBedrooms from house where address = '123 ABC St')
OR NumBath > 5`


