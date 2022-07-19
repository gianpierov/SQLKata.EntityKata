# SQLKata.EntityKata
## Entity Modeler using SQLKata

The idea is to focus the code using only entities names instead of SQL queries to model the data,
to reduce errors abstract the logic.
The library is based on SQLKata and does all the translations under the hood. 

Especially useful when entities and database columns have different names.
If a property doesn't have the field attribute it will be skipped.

This is the first release for optimization and testing. 

## More methods (and examples) will be added in the future.

Using entity Person that represents a record in table People
``` cs

// identity class is seperated for convenience
[Table("People")]
public class PersonIdentity
{
    [Identity]
    [Field("PersonId")]
    public int Id { get; set; }
}

[Table("People")]
public class Person : PersonIdentity 
{
    [Field("PersonName")]
    public string FirstName { get; set; }
    [Field("PersonLastName")]
    public string LastName { get; set; }
    [Field("PersonAge")]
    public int Age { get; set; }
    [Field("PersonPhone")]
    public string Phone { get; set; }
    [Field("PersonEmail")]
    public string Email { get; set; }
}

var env = new EntityKataManager(new MySqlConnection("Server=localhost;Database=entitkatatest;Uid=root;Pwd=;")
    , new MySqlCompiler());

var peopleManager = env.Query<Person>();

var people = peopleManager.Get();

foreach(var person in people) {
    Console.WriteLine("{0} {1}", person.FirstName, person.LastName);
}

// if you have a separated Id class you can use it, otherwise 
// you can use an anonymous class
// Both of the following are valid    
people = peopleManager.Where(new Person {Id = 2}).Get();
people = peopleManager.Where(new {Id = 2}).Get();

var insertedRows = peopleManager.Insert(new Person
{
    Age = 33,
    Email = "onon@test.com",
    FirstName = "Marlon",
    LastName = "Bereny",
    Phone = "374873434"
});

Console.WriteLine("Inserted rows: " + insertedRows);

var deletedRows = peopleManager.Where(new {FirstName = "Marlon"}).Delete();

Console.WriteLine("Deleted rows: " + deletedRows);

people = peopleManager
    .OrderByDesc(nameof(Person.LastName))
    .Get();

foreach(var person in people) {
    Console.WriteLine("{0} {1}", person.FirstName, person.LastName);
}
```
