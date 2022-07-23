# SQLKata.EntityKata
## Entity Modeler using SQLKata

The idea is to focus the code using only entities names instead of SQL queries to model the data,
to reduce errors abstract the logic.
The library is based on SQLKata and does all the translations under the hood. 

Especially useful when entities and database columns have different names.
If a property doesn't have the field attribute it will be skipped.

This is the first release for optimization and testing. 

## More methods (and examples) will be added soon.

Using entity Person that represents a record in table People
``` cs
// a separated class form identity can be convenient
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

var env = new EntityKataService(new MySqlConnection("Server=localhost;Database=entitkatatest;Uid=root;Pwd=;")
    , new MySqlCompiler());

var peopleManager = env.New<Person>();

var people = peopleManager.Get(); // select all 
people = peopleManager.Paginate(1, 10); // select first 10 people

foreach (var person in people)
{
    Console.WriteLine("{0} {1}", person.FirstName, person.LastName);
}

// if you have a separated Id class you can use it, otherwise 
// you can use an anonymous class
// Both of the following are valid    
people = peopleManager.Where(new Person {Id = 2}).Get();
people = peopleManager.Where(new {Id = 2}).Get();
people = peopleManager.Where(new {Id = new GreaterThan(2)}).Get();
people = peopleManager.Where(new {Id = new GreaterThan {Value = 2}}).Get();

people = peopleManager
    .OrderBy(nameof(Person.LastName))
    .Where(new {FirstName = "Gianpiero"})
    .Get();

var insertedRows = peopleManager.Insert(new Person
{
    Age = 33,
    Email = "onon@test.com",
    FirstName = "Marlon",
    LastName = "Bereny",
    Phone = "374873434"
});

Console.WriteLine("Inserted rows: " + insertedRows);

// you can use the same manager for the same type 
// you don't need to create a new one

peopleManager
    .Where(new PersonIdentity {Id = 2})
    .Update(new
    {
        FirstName = "John",
        LastName = "Smith"
    });

if (peopleManager.Where(new {LastName = "Bereny"}).Exists())
{
    Console.WriteLine("Found Bereny");

    var deletedRows = peopleManager.Where(new {FirstName = "Marlon"}).Delete();
    Console.WriteLine("Deleted rows: " + deletedRows);
}

var firstPersonSmith = peopleManager
    .Where(new {LastName = "Smith"})
    .FirstOrDefault();

if (firstPersonSmith != null) Console.WriteLine($"{firstPersonSmith.FirstName} is the first person last name Smith");

people = peopleManager
    .OrderByDesc(nameof(Person.LastName))
    .Get();

// you can also use an object and concatenate different orders
people = peopleManager.Order(new
{
    LastName = Ordering.Ascending,
    FirstName = Ordering.Descending
}).Get();

foreach (var person in people) Console.WriteLine("{0} {1}", person.FirstName, person.LastName);


```
