namespace SchemaGenerator.Models
{
    public class ApiParameterDescription
    {
        public ApiParameterDescription(string name, string location, ParameterDescriptor param)
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