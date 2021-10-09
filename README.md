## About HotSauceDB
HotsauceDB is a hobby project written in C#/.NET CORE. It is a hobby project and is not intended for production use. 

HotsauceDB implements its own SQL dialect, which is not ANSI compliant.

#### ACID Compliance
- Atomicity - no
- Consistency - no
- Isolation - yes
- Durability - no

## Getting Started

#### Create a Model

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


## Supported Queries

Only inserts, reads, and updates are supported at this time. HotsauceDB supports mutlithreaded 
applications.

The following read operations are all valid for HotSauceDB

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


