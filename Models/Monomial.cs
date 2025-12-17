namespace BooleanCompletenessBack.Models
{
    public class Monomial
    {
        public int[] ParamsIndices { get; set; }

        public bool Present { get; set; }

        // Строковое представление монома, например, "x1x3"
        public string Value { get; set; }
    }
}
