using System;

namespace EntityKata
{
    /// <summary>
    ///  Attribute for Fields 
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

    /// <summary>
    /// Attribute for Table names
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class TableAttribute : System.Attribute
    {
        public string Name;

        public TableAttribute(string name)
        {
            this.Name = name;
        }
    }

    /// <summary>
    /// Attribute for autoincrement fields
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class AutoIncrementAttribute : System.Attribute
    {
    }
}