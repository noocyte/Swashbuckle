using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using SchemaGenerator.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SchemaGenerator
{
    public class SchemaRegistry : ISchemaRegistry
    {
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        private readonly IContractResolver _contractResolver;

        private IDictionary<Type, SchemaInfo> _referencedTypes;
        private class SchemaInfo
        {
            public string SchemaId;
            public Schema Schema;
        }

        public SchemaRegistry()
        {
            _jsonSerializerSettings = new JsonSerializerSettings();
            _contractResolver = new DefaultContractResolver();
            _referencedTypes = new Dictionary<Type, SchemaInfo>();
            Definitions = new Dictionary<string, Schema>();
        }

        public IEnumerable<Parameter> CreateParameters(ApiParameterDescription paramDesc)
        {
            var parameter = new Parameter
            {
                @in = paramDesc.Location,
                name = paramDesc.Name
            };

            if (paramDesc.ParameterDescriptor == null)
            {
                parameter.type = "string";
                parameter.required = true;
                return HandleFromUriParams.Apply(parameter, this, paramDesc);
            }

            parameter.required = paramDesc.Location.Equals("path", StringComparison.InvariantCultureIgnoreCase) || !paramDesc.ParameterDescriptor.IsOptional;
            parameter.@default = paramDesc.ParameterDescriptor.DefaultValue;

            var schema = GetOrRegister(paramDesc.ParameterDescriptor.ParameterType);
            if (parameter.@in == "body")
                parameter.schema = schema;
            else
                parameter.PopulateFrom(schema);

            return HandleFromUriParams.Apply(parameter, this, paramDesc);
        }

        public Schema GetOrRegister(Type type)
        {
            var schema = CreateInlineSchema(type);

            // Ensure Schema's have been fully generated for all referenced types
            while (_referencedTypes.Any(entry => entry.Value.Schema == null))
            {
                var typeMapping = _referencedTypes.First(entry => entry.Value.Schema == null);
                var schemaInfo = typeMapping.Value;

                schemaInfo.Schema = CreateDefinitionSchema(typeMapping.Key);
                Definitions.Add(schemaInfo.SchemaId, schemaInfo.Schema);
            }

            return schema;
        }

        public IDictionary<string, Schema> Definitions { get; private set; }

        private Schema CreateInlineSchema(Type type, string description = null)
        {
            var jsonContract = _contractResolver.ResolveContract(type);

            if (jsonContract is JsonPrimitiveContract)
                return CreatePrimitiveSchema((JsonPrimitiveContract)jsonContract, description);

            if (jsonContract is JsonDictionaryContract dictionaryContract)
                return dictionaryContract.IsSelfReferencing()
                    ? CreateRefSchema(type)
                    : CreateDictionarySchema(dictionaryContract);

            if (jsonContract is JsonArrayContract arrayContract)
                return arrayContract.IsSelfReferencing()
                    ? CreateRefSchema(type)
                    : CreateArraySchema(arrayContract);

            if (jsonContract is JsonObjectContract objectContract && objectContract.IsInferrable())
                return CreateRefSchema(type);

            // Fallback to abstract "object"
            return CreateRefSchema(typeof(object));
        }

        private Schema CreateDefinitionSchema(Type type)
        {
            var jsonContract = _contractResolver.ResolveContract(type);

            if (jsonContract is JsonDictionaryContract)
                return CreateDictionarySchema((JsonDictionaryContract)jsonContract);

            if (jsonContract is JsonArrayContract)
                return CreateArraySchema((JsonArrayContract)jsonContract);

            if (jsonContract is JsonObjectContract)
                return CreateObjectSchema((JsonObjectContract)jsonContract);

            throw new InvalidOperationException(
                String.Format("Unsupported type - {0} for Defintitions. Must be Dictionary, Array or Object", type));
        }

        private Schema CreatePrimitiveSchema(JsonPrimitiveContract primitiveContract, string description)
        {
            var type = Nullable.GetUnderlyingType(primitiveContract.UnderlyingType) ?? primitiveContract.UnderlyingType;

            if (type.IsEnum)
                return CreateEnumSchema(primitiveContract, type, description);

            switch (type.FullName)
            {
                case "System.Int16":
                case "System.UInt16":
                case "System.Int32":
                case "System.UInt32":
                    return new Schema { type = "integer", format = "int32", description = description };
                case "System.Int64":
                case "System.UInt64":
                    return new Schema { type = "integer", format = "int64", description = description };
                case "System.Single":
                    return new Schema { type = "number", format = "float", description = description };
                case "System.Double":
                case "System.Decimal":
                    return new Schema { type = "number", format = "double", description = description };
                case "System.Byte":
                case "System.SByte":
                    return new Schema { type = "string", format = "byte", description = description };
                case "System.Boolean":
                    return new Schema { type = "boolean", description = description };
                case "System.DateTime":
                case "System.DateTimeOffset":
                    return new Schema { type = "string", format = "date-time", description = description };
                default:
                    return new Schema { type = "string", description = description };
            }
        }

        private Schema CreateEnumSchema(JsonPrimitiveContract primitiveContract, Type type, string description)
        {
            var stringEnumConverter = primitiveContract.Converter as StringEnumConverter
                ?? _jsonSerializerSettings.Converters.OfType<StringEnumConverter>().FirstOrDefault();

            if (stringEnumConverter != null)
            {
                var camelCase = (stringEnumConverter != null && stringEnumConverter.CamelCaseText);

                return new Schema
                {
                    description = description,
                    type = "string",
                    @enum = camelCase
                        ? type.GetEnumNames().Select(name => name.ToCamelCase()).ToArray()
                        : type.GetEnumNames()
                };
            }

            return new Schema
            {
                description = description,
                type = "integer",
                format = "int32",
                @enum = type.GetEnumValues().Cast<object>().ToArray()
            };
        }

        private Schema CreateDictionarySchema(JsonDictionaryContract dictionaryContract)
        {
            var valueType = dictionaryContract.DictionaryValueType ?? typeof(object);
            return new Schema
            {
                type = "object",
                additionalProperties = CreateInlineSchema(valueType)
            };
        }

        private Schema CreateArraySchema(JsonArrayContract arrayContract)
        {
            var itemType = arrayContract.CollectionItemType ?? typeof(object);
            return new Schema
            {
                type = "array",
                items = CreateInlineSchema(itemType)
            };
        }

        private Schema CreateObjectSchema(JsonObjectContract jsonContract)
        {
            var properties = jsonContract.Properties
                .Where(p => !p.Ignore())
                .Where(p => !p.IsObsolete())
                .ToDictionary(
                    prop => prop.PropertyName,
                    prop =>
                    {
                        var description = prop.GetDescription();
                        return CreateInlineSchema(prop.PropertyType, description).WithValidationProperties(prop);
                    }
                );

            var required = jsonContract.Properties.Where(prop => prop.IsRequired())
                .Select(propInfo => propInfo.PropertyName)
                .ToList();

            var schema = new Schema
            {
                required = required.Any() ? required : null, // required can be null but not empty
                properties = properties,
                type = "object"
            };

            return schema;
        }

        private Schema CreateRefSchema(Type type)
        {
            if (!_referencedTypes.ContainsKey(type))
            {
                var schemaId = type.FriendlyId();
                if (_referencedTypes.Any(entry => entry.Value.SchemaId == schemaId))
                {
                    var conflictingType = _referencedTypes.First(entry => entry.Value.SchemaId == schemaId).Key;
                    throw new InvalidOperationException(String.Format(
                        "Conflicting schemaIds: Duplicate schemaIds detected for types {0} and {1}. " +
                        "See the config setting - \"UseFullTypeNameInSchemaIds\" for a potential workaround",
                        type.FullName, conflictingType.FullName));
                }

                _referencedTypes.Add(type, new SchemaInfo { SchemaId = schemaId });
            }

            return new Schema { @ref = "#/definitions/" + _referencedTypes[type].SchemaId };
        }
    }
}