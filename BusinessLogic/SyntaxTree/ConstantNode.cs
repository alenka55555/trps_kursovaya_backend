namespace BooleanCompletenessBack.BusinessLogic.SyntaxTree
{
    public class ConstantNode : ExpressionNode
    {
        public bool Value { get; }

        public ConstantNode(bool value)
        {
            Value = value;
        }

        public override string ToString(Dictionary<string, int> precedence)
        {
            return Value ? "1" : "0";
        }

        public override List<string> GetVariables()
        {
            return new List<string>();
        }

        public override ExpressionNode Clone()
        {
            return new ConstantNode(Value);
        }
    }
}
