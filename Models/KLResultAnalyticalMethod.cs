namespace BooleanCompletenessBack.Models
{
    public class KLResultAnalyticalMethod : BaseKResult
    {
        public string SourceExpression { get; set; }
        public List<ExpressionChange> RevealingComplexOperators { get; set; }
        public List<ExpressionChange> DnfRetrieving { get; set; }
        public List<ExpressionChange> PdnfRetrieving { get; set; }
        public List<ExpressionChange> AnfRetrieving { get; set; }
    }
}
