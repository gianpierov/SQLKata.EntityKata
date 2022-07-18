namespace EntityKata;

/// <summary>
///  Attributi per la gestione Entity Kata 
/// </summary>
[System.AttributeUsage(AttributeTargets.Property)]
public class FieldAttribute : System.Attribute  
{  
    public string Name;  
  
    public FieldAttribute(string name)  
    {  
        this.Name = name;  
    }  
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class TableAttribute : System.Attribute  
{
    public string Name;  
    
    public TableAttribute(string name)  
    {  
        this.Name = name;  
    }  
}

[AttributeUsage(AttributeTargets.Property)]
public class IdentityAttribute : System.Attribute  
{

}