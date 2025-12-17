namespace BooleanCompletenessBack.Models
{
    // ExpressionChange - описывает изменение выражения
    public class ExpressionChange
    {
        // Причина изменения выражения: e.g., раскрыты скобки
        public string Reason { get; set; }

        // Полученное выражение
        public string Expression { get; set; }

        public ExpressionChange(string reason, string expression) {
            Reason = reason;
            Expression = expression;
        }
    }
}
