using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SchemaGenerator;
using SchemaGenerator.Models;

namespace UnitTests
{
    [TestClass]
    public class DescribeSchemaRegistry
    {
        [TestMethod]
        public void ItShouldCreateOneDefinition()
        {
            var sut = new SchemaRegistry();
            var actual = sut.GetOrRegister(typeof(SomeClass));
            Assert.AreEqual(sut.Definitions.Count, 1);
        }

        [TestMethod]
        public void ItShouldCreateParameter()
        {
            var sut = new SchemaRegistry();
            var parameterDescriptor = new ParameterDescriptor(typeof(SomeClass));
            var apiParameterDescription = new ApiParameterDescription(parameterDescriptor);

            var actual = sut.CreateParameters(apiParameterDescription);
            Assert.AreEqual(sut.Definitions.Count, 1);
            Assert.AreEqual(sut.Definitions["SomeClass"].properties.Count, 2);
        }

        class SomeClass
        {
            [DocumentationDescription("Some random number dude!")]
            public int ANumber { get; set; }
            public string SomeText { get; set; }
            [JsonIgnore]
            public string JsonIgnoredProp { get; set; }
            [DocumentationIgnore]
            public string DocIgnoredProp { get; set; }
        }
    }
}
