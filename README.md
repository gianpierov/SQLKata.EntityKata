# SQLKata.EntityKata
## Entity Modeler using SQLKata

The idea is to focus the code using only entities names instead of SQL queries to model the data.
The library is based on SQLKata and does all the translations under the hood. 

Example of an entity with different columns name on db
If a property doesn't have the field attribute it will be skipped.

This is the first release for optimization and testing. 
More methods (and examples) will be added in the future.

Using entity Person that represents a record in table People
``` cs
[Table("People")]
public class Person
{
    
    [Field("Person_ID")]
    [Identity]
    public int? PersonId { get; set; }
    
    [Field("Person_Name")]
    [Identity]
    public string? PersonName { get; set; }
    
    [Field("Person_Surname")]
    [Identity]
    public string? Surname { get; set; }
        
}

var manager = new EntityKataManager(connectionString);

var people = manager.Query<Person>()
     .Where(new {Name = "Gianpiero"})
     .OrderByDesc(nameof(Person.Surname))
     .Get();
     
foreach (person in people) 
{
    Console.WriteLine($"{person.Name} {person.Surname}");
}
     
int numberOfdeletedRows = manager.Query<Person>().Where(new { Surname = "Smith"}).Delete();
```
