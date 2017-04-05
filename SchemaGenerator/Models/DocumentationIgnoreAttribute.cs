using System;

namespace SchemaGenerator.Models
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class DocumentationIgnoreAttribute : Attribute
    {
    }
}
