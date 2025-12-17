namespace BooleanCompletenessBack.BusinessLogic.SyntaxTree
{
    public class BinaryNode : ExpressionNode
    {
        public string Operator { get; set; }
        public ExpressionNode Left { get; set; }
        public ExpressionNode Right { get; set; }

        public BinaryNode(string op, ExpressionNode left, ExpressionNode right)
        {
            Operator = op;
            Left = left;
            Right = right;
        }

        public override string ToString(Dictionary<string, int> precedence)
        {
            var opPrec = precedence.GetValueOrDefault(Operator, 0);
            var leftStr = Left.ToString(precedence);
            var rightStr = Right.ToString(precedence);

            // Добавляем скобки на основе приоритетов и ассоциативности
            if (Left is BinaryNode leftBin && (precedence.GetValueOrDefault(leftBin.Operator, 0) < opPrec ||
                (precedence.GetValueOrDefault(leftBin.Operator, 0) == opPrec && IsRightAssoc(Operator))))
            {
                leftStr = $"({leftStr})";
            }

            if (Right is BinaryNode rightBin && (precedence.GetValueOrDefault(rightBin.Operator, 0) < opPrec ||
                (precedence.GetValueOrDefault(rightBin.Operator, 0) == opPrec && !IsRightAssoc(Operator) && !IsFlatAssociative(Operator))))
            {
                rightStr = $"({rightStr})";
            }

            if (Operator == "∧")
            {
                return $"{leftStr}\\, {rightStr}";
            }

            return $"{leftStr} {Operator} {rightStr}";
        }

        public override List<string> GetVariables()
        {
            var vars = Left.GetVariables();
            vars.AddRange(Right.GetVariables());
            return vars.Distinct().ToList();
        }

        private bool IsRightAssoc(string op)
        {
            return op == "→" || op == "↛";
        }
 
        private bool IsFlatAssociative(string op)
        {
            return op == "∧" || op == "∨";
        }

        public override ExpressionNode Clone()
        {
            return new BinaryNode(Operator, Left.Clone(), Right.Clone());
        }
    }
}
