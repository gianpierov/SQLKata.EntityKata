# SQLKata.EntityKata
## Entity Modeler using SQLKata

Example of an entity with diffent columns name on db

Using entity Person that represents a record in table People
```
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
