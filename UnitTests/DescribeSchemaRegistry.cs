using Microsoft.VisualStudio.TestTools.UnitTesting;
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

            var actual = sut.CreateParameter(apiParameterDescription);
            Assert.AreEqual(sut.Definitions.Count, 1);
        }


        class SomeClass
        {
            public int ANumber { get; set; }
            public string SomeText { get; set; }
        }
    }
}
