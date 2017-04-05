using System;

namespace SchemaGenerator.Models
{
    public class ParameterDescriptor
    {
        public ParameterDescriptor(Type parameterType, bool isOptional = true, object defaultValue = null)
        {
            ParameterType = parameterType;
            IsOptional = isOptional;
            DefaultValue = defaultValue;
        }
        public bool IsOptional { get; set; }
        public object DefaultValue { get; set; }
        public Type ParameterType { get; set; }

    }
}