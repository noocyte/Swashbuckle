using Newtonsoft.Json.Serialization;
using SchemaGenerator.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace SchemaGenerator
{
    public static class JsonPropertyExtensions
    {
        public static bool IsRequired(this JsonProperty jsonProperty)
        {
            return jsonProperty.HasAttribute<RequiredAttribute>();
        }

        public static bool IsObsolete(this JsonProperty jsonProperty)
        {
            return jsonProperty.HasAttribute<ObsoleteAttribute>();
        }

        public static bool Ignore(this JsonProperty jsonProperty)
        {
            return jsonProperty.Ignored || jsonProperty.HasAttribute<DocumentationIgnoreAttribute>();
        }

        public static string GetDescription(this JsonProperty jsonProperty)
        {
            if (!jsonProperty.HasAttribute<DocumentationDescriptionAttribute>()) return null;
            var propInfo = jsonProperty.PropertyInfo();

            foreach (var attr in propInfo.CustomAttributes)
            {
                if (attr.AttributeType == typeof(DocumentationDescriptionAttribute))
                {
                    return attr.ConstructorArguments[0].Value.ToString();
                }
            }

            return null;
        }

        public static bool HasAttribute<T>(this JsonProperty jsonProperty)
        {
            var propInfo = jsonProperty.PropertyInfo();
            return propInfo != null && Attribute.IsDefined(propInfo, typeof(T));
        }

        public static PropertyInfo PropertyInfo(this JsonProperty jsonProperty)
        {
            if (jsonProperty.UnderlyingName == null) return null;
            return jsonProperty.DeclaringType.GetProperty(jsonProperty.UnderlyingName, jsonProperty.PropertyType);
        }
    }
}
