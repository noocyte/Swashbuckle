using System;

namespace SchemaGenerator.Models
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class DocumentationDescriptionAttribute : Attribute
    {
        public string Description { get; set; }
        public DocumentationDescriptionAttribute(string description)
        {
            Description = description;
        }
    }
}
