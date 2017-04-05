namespace SchemaGenerator.Models
{
    public class ApiParameterDescription
    {
        public ApiParameterDescription(ParameterDescriptor param, string name = "", string location = "query")
        {
            Name = name;
            Location = location;
            ParameterDescriptor = param;
        }
        public ParameterDescriptor ParameterDescriptor { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
    }
}