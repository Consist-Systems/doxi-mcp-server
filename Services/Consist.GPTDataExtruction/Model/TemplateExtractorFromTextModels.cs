namespace Consist.GPTDataExtruction.Model
{
    public class OpenAIResponse
    {
        public List<ResponseOutput> output { get; set; }
    }

    public class ResponseOutput
    {
        public string id { get; set; }
        public string type { get; set; }
        public string status { get; set; }
        public List<ResponseContent> content { get; set; }
    }

    public class ResponseContent
    {
        public string type { get; set; }

        // This is where your JSON string is
        public string text { get; set; }
    }
}
