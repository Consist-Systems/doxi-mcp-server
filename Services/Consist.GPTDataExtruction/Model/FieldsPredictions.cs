namespace Consist.GPTDataExtruction.Model
{
    public class FieldsPredictions
    {
        public List<FieldPrediction> Fields { get; set; }
    }

    public class FieldPrediction
    {
        public int FieldNumber { get; set; }
        public string Label { get; set; }
        public string Signer { get; set; }
    }
}
