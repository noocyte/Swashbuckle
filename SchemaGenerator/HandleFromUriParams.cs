using SchemaGenerator.Models;
using System.Collections.Generic;
using System.Linq;

namespace SchemaGenerator
{
    internal static class HandleFromUriParams
    {
        public static IList<Parameter> Apply(Parameter parameter, SchemaRegistry schemaRegistry, ApiParameterDescription apiDescription)
        {
            if (parameter == null) return null;

            var parameters = new List<Parameter> { parameter };
            HandleFromUriArrayParams(parameters);
            HandleFromUriObjectParams(parameters, schemaRegistry, apiDescription);

            return parameters;
        }
        public static IList<Parameter> Apply(IList<Parameter> parameters, SchemaRegistry schemaRegistry, ApiParameterDescription apiDescription)
        {
            if (parameters == null || parameters.Count == 0) return parameters;

            HandleFromUriArrayParams(parameters);
            HandleFromUriObjectParams(parameters, schemaRegistry, apiDescription);

            return parameters;
        }

        private static void HandleFromUriArrayParams(IList<Parameter> parameters)
        {
            var fromUriArrayParams = parameters
                .Where(param => param.@in == "query" && param.type == "array")
                .ToArray();

            foreach (var param in fromUriArrayParams)
            {
                param.collectionFormat = "multi";
            }
        }

        private static void HandleFromUriObjectParams(IList<Parameter> parameters, SchemaRegistry schemaRegistry, ApiParameterDescription apiParameterDescriptor)
        {
            var fromUriObjectParams = parameters
                .Where(param => param.@in == "query" && param.type == null)
                .ToArray();

            foreach (var objectParam in fromUriObjectParams)
            {
                var type = apiParameterDescriptor.ParameterDescriptor.ParameterType;

                var refSchema = schemaRegistry.GetOrRegister(type);
                var schema = schemaRegistry.Definitions[refSchema.@ref.Replace("#/definitions/", "")];

                var qualifier = string.IsNullOrEmpty(objectParam.name) ? "" : (objectParam.name + ".");
                ExtractAndAddQueryParams(schema, qualifier, objectParam.required, schemaRegistry, parameters);
                parameters.Remove(objectParam);
            }
        }

        private static void ExtractAndAddQueryParams(
            Schema sourceSchema,
            string sourceQualifier,
            bool? sourceRequired,
            SchemaRegistry schemaRegistry,
            IList<Parameter> operationParams)
        {
            foreach (var entry in sourceSchema.properties)
            {
                var propertySchema = entry.Value;
                if (propertySchema.readOnly == true) continue;

                var required = (sourceRequired == true)
                    && sourceSchema.required != null && sourceSchema.required.Contains(entry.Key);

                if (propertySchema.@ref != null)
                {
                    var schema = schemaRegistry.Definitions[propertySchema.@ref.Replace("#/definitions/", "")];
                    ExtractAndAddQueryParams(
                        schema,
                        sourceQualifier + entry.Key.ToCamelCase() + ".",
                        required,
                        schemaRegistry,
                        operationParams);
                }
                else
                {
                    var param = new Parameter
                    {
                        name = sourceQualifier + entry.Key.ToCamelCase(),
                        @in = "query",
                        required = required,
                        description = entry.Value.description
                    };
                    param.PopulateFrom(entry.Value);
                    if (param.type == "array")
                        param.collectionFormat = "multi";
                    operationParams.Add(param);
                }
            }
        }
    }
}
