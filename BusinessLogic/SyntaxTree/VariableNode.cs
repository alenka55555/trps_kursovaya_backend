namespace BooleanCompletenessBack.BusinessLogic.SyntaxTree
{
    public class VariableNode : ExpressionNode
    {
        public string Name { get; }

        public VariableNode(string name)
        {
            Name = name;
        }

        public override string ToString(Dictionary<string, int> precedence)
        {
            if (Name.Length > 1)
            {
                // x_{12}

                // x --- {Name.Substring(0, 1)}
                // _ --- _
                // { --- {{
                // 12 --- {Name.Substring(1)}
                // } --- }}
                return $"{Name.Substring(0, 1)}_{{{Name.Substring(1)}}}";
            }
            return Name;
        }

        public override List<string> GetVariables()
        {
            return new List<string> { Name };
        }

        public override ExpressionNode Clone()
        {
            return new VariableNode(Name);
        }
    }
}
