using System;
using System.Collections.Generic;
using SchemaGenerator.Models;

namespace SchemaGenerator
{
    public interface ISchemaRegistry
    {
        IDictionary<string, Schema> Definitions { get; }

        IEnumerable<Parameter> CreateParameters(ApiParameterDescription paramDesc);
        Schema GetOrRegister(Type type);
    }
}