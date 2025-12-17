namespace BooleanCompletenessBack.BusinessLogic.SyntaxTree
{
    public class UnaryNode : ExpressionNode
    {
        public string Operator { get; }
        public ExpressionNode Operand { get; set; }

        public UnaryNode(string op, ExpressionNode operand)
        {
            Operator = op;
            Operand = operand;
        }

        public override string ToString(Dictionary<string, int> precedence)
        {
            var opPrec = precedence.GetValueOrDefault(Operator, 0);
            var childStr = Operand.ToString(precedence);
            return $"\\overline {{{childStr}}}";
        }

        public override List<string> GetVariables()
        {
            return Operand.GetVariables();
        }

        public override ExpressionNode Clone()
        {
            return new UnaryNode(Operator, Operand.Clone());
        }
    }
}
