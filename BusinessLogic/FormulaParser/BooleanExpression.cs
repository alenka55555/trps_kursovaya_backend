using BooleanCompletenessBack.BusinessLogic.SyntaxTree;

namespace BooleanCompletenessBack.BusinessLogic.FormulaParser
{
    public class BooleanExpression
    {
        private List<Token> _rpn;

        // Переменные в порядке появления в выражении
        private List<string> _variablesInOrder;
        private bool _changed;

        public bool Changed
        {
            get
            {
                return _changed;
            }
        }

        public BooleanExpression(string formula, int maxVariablesLimit)
        {
            var lexer = new Lexer(formula);
            var tokens = lexer.Tokenize();

            // Собрать переменные в порядке их появления в выражении
            _variablesInOrder = tokens
                .Where(t => t.Type == TokenType.Variable)
                .Select(t => t.Value)
                // Уникальные, но сохраняем очередность появления
                .Distinct()
                .ToList();

            if (_variablesInOrder.Count > maxVariablesLimit)
            {
                throw new ClientException($"Формула не должна содержать более чем {maxVariablesLimit} переменных");
            }

            var parser = new Parser(tokens);
            _rpn = parser.ToRPN();
        }

        // Сбрасывает флаг, который хранит то,
        // было ли выражение изменено.
        public void ClearChanged()
        {
            _changed = false;
        }

        public List<string> GetVariables()
        {
            return new List<string>(_variablesInOrder);
        }

        public bool Evaluate(List<string> paramNames, List<bool> paramValues)
        {
            if (paramNames.Count != paramValues.Count)
                throw new ArgumentException("Param names and values must match in count");

            var valueDict = new Dictionary<string, bool>();
            for (int i = 0; i < paramNames.Count; i++)
            {
                valueDict[paramNames[i]] = paramValues[i];
            }

            var stack = new Stack<bool>();

            foreach (var token in _rpn)
            {
                if (token.Type == TokenType.Const)
                {
                    stack.Push(token.Value == "1");
                }
                else if (token.Type == TokenType.Variable)
                {
                    if (!valueDict.TryGetValue(token.Value, out bool val))
                        throw new Exception($"Missing value for variable {token.Value}");
                    stack.Push(val);
                }
                else if (token.Type == TokenType.Operator)
                {
                    if (_unaryOps.Contains(token.Value))
                    {
                        if (stack.Count < 1) throw new Exception("Invalid RPN");
                        bool a = stack.Pop();
                        stack.Push(EvalUnary(token.Value, a));
                    }
                    else
                    {
                        if (stack.Count < 2) throw new Exception("Invalid RPN");
                        bool b = stack.Pop();
                        bool a = stack.Pop();
                        stack.Push(EvalBinary(token.Value, a, b));
                    }
                }
            }

            if (stack.Count != 1) throw new Exception("Invalid expression evaluation");
            return stack.Pop();
        }

        private static readonly HashSet<string> _unaryOps = new HashSet<string> { "¬" };

        private bool EvalUnary(string op, bool a)
        {
            return op == "¬" ? !a : throw new Exception("Unknown unary op");
        }

        private bool EvalBinary(string op, bool a, bool b)
        {
            switch (op)
            {
                case "∧": return a && b;
                case "∨": return a || b;
                case "⊕": return a ^ b;
                case "→": return !a || b;
                case "↔": return a == b;
                case "↓": return !(a || b); // НЕ ИЛИ
                case "↑": return !(a && b); // НЕ И
                case "↛": return a && !b; // НЕ импликация
                case "↮": return a != b; // НЕ эквивалентность
                default: throw new Exception($"Unknown operator {op}");
            }
        }

        public List<List<bool>> GetTruthTable(List<string> paramNames = null)
        {
            var vars = paramNames ?? _variablesInOrder;
            var numVars = vars.Count;
            var numRows = 1 << numVars; // 2^n
            var table = new List<List<bool>>();

            for (int i = 0; i < numRows; i++)
            {
                var row = new List<bool>();
                var values = new List<bool>();

                for (int j = 0; j < numVars; j++)
                {
                    bool val = ((i >> (numVars - 1 - j)) & 1) == 1;
                    values.Add(val);
                    row.Add(val);
                }

                bool result = Evaluate(vars, values);
                row.Add(result);
                table.Add(row);
            }

            return table;
        }

        private ExpressionNode BuildAstFromRpn()
        {
            var stack = new Stack<ExpressionNode>();

            foreach (var token in _rpn)
            {
                if (token.Type == TokenType.Variable)
                {
                    stack.Push(new VariableNode(token.Value));
                }
                else if (token.Type == TokenType.Const)
                {
                    stack.Push(new ConstantNode(token.Value == "1"));
                }
                else if (token.Type == TokenType.Operator)
                {
                    if (_unaryOps.Contains(token.Value))
                    {
                        if (stack.Count < 1) throw new Exception("Invalid RPN");
                        var operand = stack.Pop();
                        stack.Push(new UnaryNode(token.Value, operand));
                    }
                    else
                    {
                        if (stack.Count < 2) throw new Exception("Invalid RPN");
                        var right = stack.Pop();
                        var left = stack.Pop();
                        stack.Push(new BinaryNode(token.Value, left, right));
                    }
                }
            }

            if (stack.Count != 1) throw new Exception("Invalid expression");
            return stack.Pop();
        }

        public bool Replace(string oper)
        {
            ClearChanged();
            var ast = BuildAstFromRpn();
            ast = ReplaceInAst(ast, oper);
            // Заново генерируем RPN из модифицированного AST (для вычисления)
            _rpn = AstToRpn(ast);
            return Changed;
        }

        private ExpressionNode ReplaceInAst(ExpressionNode node, string oper)
        {
            if (node is VariableNode)
            {
                return node;
            }
            else if (node is UnaryNode unary)
            {
                unary.Operand = ReplaceInAst(unary.Operand, oper);
                return unary;
            }
            else if (node is BinaryNode binary)
            {
                binary.Left = ReplaceInAst(binary.Left, oper);
                binary.Right = ReplaceInAst(binary.Right, oper);

                if (binary.Operator == oper)
                {
                    _changed = true;

                    if (oper == "↛")
                    {
                        return new UnaryNode("¬",
                            new BinaryNode("→", binary.Left, binary.Right));
                    }
                    if (oper == "↮")
                    {
                        return new UnaryNode("¬",
                            new BinaryNode("↔", binary.Left, binary.Right));
                    }

                    if (oper == "↓")
                    {
                        return new UnaryNode("¬",
                            new BinaryNode("∨", binary.Left, binary.Right));
                    }
                    if (oper == "↑")
                    {
                        return new UnaryNode("¬",
                            new BinaryNode("∧", binary.Left, binary.Right));
                    }
                    if (oper == "→")
                    {
                        // Заменяем A → B на ¬A ∨ B
                        var notLeft = new UnaryNode("¬", binary.Left);
                        return new BinaryNode("∨", notLeft, binary.Right);
                    }
                    if (oper == "↔")
                    {
                        return new BinaryNode("∨",
                            new BinaryNode("∧", binary.Left, binary.Right),
                            new BinaryNode("∧",
                                new UnaryNode("¬", binary.Left),
                                new UnaryNode("¬", binary.Right)));
                    }
                    if (oper == "⊕")
                    {
                        return new BinaryNode("∨",
                            new BinaryNode("∧", binary.Left, new UnaryNode("¬", binary.Right)),
                            new BinaryNode("∧", new UnaryNode("¬", binary.Left), binary.Right));
                    }
                }

                return binary;
            }
            return node;
        }

        private List<Token> AstToRpn(ExpressionNode node)
        {
            var rpn = new List<Token>();

            if (node is VariableNode varNode)
            {
                rpn.Add(new Token(TokenType.Variable, varNode.Name));
            }
            else if (node is ConstantNode constNode)
            {
                rpn.Add(new Token(TokenType.Const, constNode.Value ? "1" : "0"));
            }
            else if (node is UnaryNode unary)
            {
                rpn.AddRange(AstToRpn(unary.Operand));
                rpn.Add(new Token(TokenType.Operator, unary.Operator));
            }
            else if (node is BinaryNode binary)
            {
                rpn.AddRange(AstToRpn(binary.Left));
                rpn.AddRange(AstToRpn(binary.Right));
                rpn.Add(new Token(TokenType.Operator, binary.Operator));
            }

            return rpn;
        }

        public override string ToString()
        {
            var ast = BuildAstFromRpn();
            return ast.ToString(Parser.OperatorsPrecedence);
        }

        public bool ApplyDoubleNegationOnce()
        {
            ClearChanged();
            var ast = BuildAstFromRpn();
            var (newAst, applied) = ApplyDoubleNegationInAstOnce(ast);
            if (applied)
            {
                _rpn = AstToRpn(newAst);
                _changed = true;
            }
            return _changed;
        }

        private (ExpressionNode, bool) ApplyDoubleNegationInAstOnce(ExpressionNode node)
        {
            if (node is ConstantNode)
                return (node, false);

            if (node is VariableNode)
                return (node, false);

            if (node is UnaryNode unary)
            {
                if (unary.Operator == "¬" && unary.Operand is UnaryNode innerUnary && innerUnary.Operator == "¬")
                {
                    return (innerUnary.Operand, true);
                }

                var (newOperand, applied) = ApplyDoubleNegationInAstOnce(unary.Operand);
                if (applied)
                {
                    unary.Operand = newOperand;
                    return (unary, true);
                }
                return (unary, false);
            }

            if (node is BinaryNode binary)
            {
                var (newLeft, appliedLeft) = ApplyDoubleNegationInAstOnce(binary.Left);
                if (appliedLeft)
                {
                    binary.Left = newLeft;
                    return (binary, true);
                }

                var (newRight, appliedRight) = ApplyDoubleNegationInAstOnce(binary.Right);
                binary.Right = newRight;
                return (binary, appliedRight);
            }

            return (node, false);
        }

        public bool ApplyDeMorganOnce()
        {
            ClearChanged();
            var ast = BuildAstFromRpn();
            var (newAst, applied) = ApplyDeMorganInAstOnce(ast);
            if (applied)
            {
                _rpn = AstToRpn(newAst);
                _changed = true;
            }
            return _changed;
        }

        private (ExpressionNode, bool) ApplyDeMorganInAstOnce(ExpressionNode node)
        {
            if (node is ConstantNode)
                return (node, false);

            if (node is VariableNode)
                return (node, false);

            if (node is UnaryNode unary)
            {
                if (unary.Operator == "¬" && unary.Operand is BinaryNode innerBinary)
                {
                    string innerOp = innerBinary.Operator;
                    if (innerOp == "∧" || innerOp == "∨")
                    {
                        string newOp = innerOp == "∧" ? "∨" : "∧";
                        var newLeft = new UnaryNode("¬", innerBinary.Left.Clone());
                        var newRight = new UnaryNode("¬", innerBinary.Right.Clone());
                        var newBinary = new BinaryNode(newOp, newLeft, newRight);
                        return (newBinary, true);
                    }
                }

                var (newOperand, applied) = ApplyDeMorganInAstOnce(unary.Operand);
                if (applied)
                {
                    unary.Operand = newOperand;
                    return (unary, true);
                }
                return (unary, false);
            }

            if (node is BinaryNode binary)
            {
                var (newLeft, appliedLeft) = ApplyDeMorganInAstOnce(binary.Left);
                if (appliedLeft)
                {
                    binary.Left = newLeft;
                    return (binary, true);
                }

                var (newRight, appliedRight) = ApplyDeMorganInAstOnce(binary.Right);
                binary.Right = newRight;
                return (binary, appliedRight);
            }

            return (node, false);
        }

        public bool ApplyDistributiveOnce()
        {
            ClearChanged();
            var ast = BuildAstFromRpn();
            var (newAst, applied) = ApplyDistributiveInAstOnce(ast);
            if (applied)
            {
                _rpn = AstToRpn(newAst);
                _changed = true;
            }
            return _changed;
        }

        private (ExpressionNode, bool) ApplyDistributiveInAstOnce(ExpressionNode node)
        {
            if (node is ConstantNode)
                return (node, false);

            if (node is VariableNode)
                return (node, false);

            if (node is UnaryNode unary)
            {
                var (newOperand, applied) = ApplyDistributiveInAstOnce(unary.Operand);
                if (applied)
                {
                    unary.Operand = newOperand;
                    return (unary, true);
                }
                return (unary, false);
            }

            if (node is BinaryNode binary)
            {
                // Проверяем, может ли к этому узлу быть применён закон
                // дистрибутивности
                if (binary.Operator == "∧")
                {
                    if (binary.Left is BinaryNode leftBin && leftBin.Operator == "∨")
                    {
                        var newLeft = new BinaryNode("∧", leftBin.Left.Clone(), binary.Right.Clone());
                        var newRight = new BinaryNode("∧", leftBin.Right.Clone(), binary.Right.Clone());
                        var newNode = new BinaryNode("∨", newLeft, newRight);
                        return (newNode, true);
                    }

                    if (binary.Right is BinaryNode rightBin && rightBin.Operator == "∨")
                    {
                        var newLeft = new BinaryNode("∧", binary.Left.Clone(), rightBin.Left.Clone());
                        var newRight = new BinaryNode("∧", binary.Left.Clone(), rightBin.Right.Clone());
                        var newNode = new BinaryNode("∨", newLeft, newRight);
                        return (newNode, true);
                    }
                }

                // Рекурсивный вызов для левого и правого операндов
                var (newLeftOuter, appliedLeft) = ApplyDistributiveInAstOnce(binary.Left);
                if (appliedLeft)
                {
                    binary.Left = newLeftOuter;
                    return (binary, true);
                }

                var (newRightOuter, appliedRight) = ApplyDistributiveInAstOnce(binary.Right);
                binary.Right = newRightOuter;
                return (binary, appliedRight);
            }

            return (node, false);
        }

        public bool ApplyIdempotenceOnce()
        {
            ClearChanged();
            var ast = BuildAstFromRpn();
            var (newAst, applied) = ApplyIdempotenceInAstOnce(ast);
            if (applied)
            {
                _rpn = AstToRpn(newAst);
                _changed = true;
            }
            return _changed;
        }

        private (ExpressionNode, bool) ApplyIdempotenceInAstOnce(ExpressionNode node)
        {
            if (node is ConstantNode)
                return (node, false);

            if (node is VariableNode)
                return (node, false);

            if (node is UnaryNode unary)
            {
                var (newOperand, applied) = ApplyIdempotenceInAstOnce(unary.Operand);
                if (applied)
                {
                    unary.Operand = newOperand;
                    return (unary, true);
                }
                return (unary, false);
            }

            if (node is BinaryNode binary)
            {
                var (newLeft, appliedLeft) = ApplyIdempotenceInAstOnce(binary.Left);
                binary.Left = newLeft;

                var (newRight, appliedRight) = ApplyIdempotenceInAstOnce(binary.Right);
                binary.Right = newRight;

                bool applied = appliedLeft || appliedRight;

                if (binary.Operator == "∧" || binary.Operator == "∨")
                {
                    var allTerms = GetTerms(binary, binary.Operator);

                    // Найти первую дублирующуюся пару
                    int? dupIndex = null;
                    for (int i = 0; i < allTerms.Count; i++)
                    {
                        for (int j = i + 1; j < allTerms.Count; j++)
                        {
                            if (NodesEqual(allTerms[i], allTerms[j]))
                            {
                                dupIndex = j;
                                goto FoundDuplicate;
                            }
                        }
                    }
                FoundDuplicate:
                    if (dupIndex.HasValue)
                    {
                        var newList = allTerms.Where((t, k) => k != dupIndex.Value).ToList();
                        ExpressionNode rebuilt;
                        if (newList.Count == 0)
                        {
                            bool isFalse = binary.Operator == "∧";
                            rebuilt = new ConstantNode(isFalse);
                        }
                        else if (newList.Count == 1)
                        {
                            rebuilt = newList[0];
                        }
                        else
                        {
                            rebuilt = newList[0];
                            for (int k = 1; k < newList.Count; k++)
                            {
                                rebuilt = new BinaryNode(binary.Operator, rebuilt, newList[k]);
                            }
                        }
                        return (rebuilt, true);
                    }
                }

                return (binary, applied);
            }

            return (node, false);
        }

        private List<ExpressionNode> GetTerms(ExpressionNode node, string op)
        {
            var list = new List<ExpressionNode>();
            if (node is BinaryNode bin && bin.Operator == op)
            {
                list.AddRange(GetTerms(bin.Left, op));
                list.AddRange(GetTerms(bin.Right, op));
            }
            else
            {
                list.Add(node);
            }
            return list;
        }

        private List<ExpressionNode> GetFlatOperands(ExpressionNode node, string op)
        {
            var list = new List<ExpressionNode>();
            if (node is BinaryNode bin && bin.Operator == op)
            {
                list.AddRange(GetFlatOperands(bin.Left, op));
                list.AddRange(GetFlatOperands(bin.Right, op));
            }
            else
            {
                list.Add(node);
            }
            return list;
        }

        private bool NodesEqual(ExpressionNode node1, ExpressionNode node2)
        {
            if (node1.GetType() != node2.GetType())
            {
                return false;
            }

            if (node1 is VariableNode var1 && node2 is VariableNode var2)
            {
                return var1.Name == var2.Name;
            }

            if (node1 is UnaryNode un1 && node2 is UnaryNode un2)
            {
                return un1.Operator == un2.Operator && NodesEqual(un1.Operand, un2.Operand);
            }

            if (node1 is BinaryNode bin1 && node2 is BinaryNode bin2)
            {
                if (bin1.Operator != bin2.Operator)
                {
                    return false;
                }

                var op = bin1.Operator;
                if (op == "∧" || op == "∨")
                {
                    var ops1 = GetFlatOperands(bin1, op).OrderBy(o => o.ToString(Parser.OperatorsPrecedence)).ToList();
                    var ops2 = GetFlatOperands(bin2, op).OrderBy(o => o.ToString(Parser.OperatorsPrecedence)).ToList();
                    if (ops1.Count != ops2.Count)
                    {
                        return false;
                    }
                    for (int i = 0; i < ops1.Count; i++)
                    {
                        if (!NodesEqual(ops1[i], ops2[i]))
                        {
                            return false;
                        }
                    }
                    return true;
                }

                return NodesEqual(bin1.Left, bin2.Left) && NodesEqual(bin1.Right, bin2.Right);
            }

            if (node1 is ConstantNode const1 && node2 is ConstantNode const2)
            {
                return const1.Value == const2.Value;
            }

            return false;
        }

        public bool ApplyContradictionOnce()
        {
            ClearChanged();
            var ast = BuildAstFromRpn();
            var (newAst, applied) = ApplyContradictionInAstOnce(ast);
            if (applied)
            {
                _rpn = AstToRpn(newAst);
                _changed = true;
            }
            return _changed;
        }

        private (ExpressionNode, bool) ApplyContradictionInAstOnce(ExpressionNode node)
        {
            if (node is ConstantNode)
            {
                return (node, false);
            }

            if (node is VariableNode)
            {
                return (node, false);
            }

            if (node is UnaryNode unary)
            {
                var (newOperand, applied) = ApplyContradictionInAstOnce(unary.Operand);
                unary.Operand = newOperand;
                return (unary, applied);
            }

            if (node is BinaryNode binary)
            {
                var (newLeft, appliedLeft) = ApplyContradictionInAstOnce(binary.Left);
                binary.Left = newLeft;

                var (newRight, appliedRight) = ApplyContradictionInAstOnce(binary.Right);
                binary.Right = newRight;

                bool applied = appliedLeft || appliedRight;

                if (binary.Operator == "∧")
                {
                    var conjuncts = GetConjuncts(binary);
                    if (conjuncts.Any(c => c is ConstantNode cc && !cc.Value))
                    {
                        return (new ConstantNode(false), true);
                    }
                    bool hasContradiction = false;
                    for (int i = 0; i < conjuncts.Count; i++)
                    {
                        for (int j = i + 1; j < conjuncts.Count; j++)
                        {
                            if (IsNegation(conjuncts[i], conjuncts[j]))
                            {
                                hasContradiction = true;
                                break;
                            }
                        }
                        if (hasContradiction)
                        {
                            break;
                        }
                    }
                    if (hasContradiction)
                    {
                        return (new ConstantNode(false), true);
                    }
                }
                else if (binary.Operator == "∨")
                {
                    if (binary.Left is ConstantNode leftConst && !leftConst.Value)
                    {
                        return (binary.Right, true);
                    }
                    if (binary.Right is ConstantNode rightConst && !rightConst.Value)
                    {
                        return (binary.Left, true);
                    }
                }

                return (binary, applied);
            }

            return (node, false);
        }

        private bool IsNegation(ExpressionNode node1, ExpressionNode node2)
        {
            if (node1 is UnaryNode un1 && un1.Operator == "¬" && NodesEqual(un1.Operand, node2))
            {
                return true;
            }
            if (node2 is UnaryNode un2 && un2.Operator == "¬" && NodesEqual(un2.Operand, node1))
            {
                return true;
            }
            return false;
        }

        public bool ApplyAbsorptionOnce()
        {
            ClearChanged();
            var ast = BuildAstFromRpn();
            var (newAst, applied) = ApplyAbsorptionInAstOnce(ast);
            if (applied)
            {
                _rpn = AstToRpn(newAst);
                _changed = true;
            }
            return _changed;
        }

        private (ExpressionNode, bool) ApplyAbsorptionInAstOnce(ExpressionNode node)
        {
            if (node is ConstantNode)
                return (node, false);

            if (node is VariableNode)
                return (node, false);

            if (node is UnaryNode unary)
            {
                var (newOperand, applied) = ApplyAbsorptionInAstOnce(unary.Operand);
                if (applied)
                {
                    unary.Operand = newOperand;
                    return (unary, true);
                }
                return (unary, false);
            }

            if (node is BinaryNode binary)
            {
                var (newLeft, appliedLeft) = ApplyAbsorptionInAstOnce(binary.Left);
                binary.Left = newLeft;

                var (newRight, appliedRight) = ApplyAbsorptionInAstOnce(binary.Right);
                binary.Right = newRight;

                bool applied = appliedLeft || appliedRight;

                if (binary.Operator == "∨")
                {
                    var terms = GetFlatOperands(binary, "∨");
                    var precedence = Parser.OperatorsPrecedence;
                    var litSets = terms.Select(t => GetLiterals(t, precedence)).ToList();

                    // Находим первую пару, в которой i поглощает j
                    // (litSets[i] является собственным подмножеством litSets[j])
                    int? absorbI = null;
                    int? absorbJ = null;
                    for (int i = 0; i < terms.Count; i++)
                    {
                        for (int j = 0; j < terms.Count; j++)
                        {
                            if (i != j && litSets[i].IsProperSubsetOf(litSets[j]))
                            {
                                absorbI = i;
                                absorbJ = j;
                                goto FoundAbsorption;
                            }
                        }
                    }
                FoundAbsorption:
                    if (absorbI.HasValue && absorbJ.HasValue)
                    {
                        // Удаляем поглощенный терм j
                        var newTerms = terms.Where((t, k) => k != absorbJ.Value).ToList();
                        ExpressionNode rebuilt;
                        if (newTerms.Count == 0)
                        {
                            // Не должно случиться для ∨
                            rebuilt = new ConstantNode(false);
                        }
                        else if (newTerms.Count == 1)
                        {
                            rebuilt = newTerms[0];
                        }
                        else
                        {
                            rebuilt = newTerms[0];
                            for (int k = 1; k < newTerms.Count; k++)
                            {
                                rebuilt = new BinaryNode("∨", rebuilt, newTerms[k]);
                            }
                        }
                        return (rebuilt, true);
                    }
                }

                return (binary, applied);
            }

            return (node, false);
        }

        private SortedSet<string> GetLiterals(ExpressionNode node, Dictionary<string, int> precedence)
        {
            var set = new SortedSet<string>();
            if (node is BinaryNode bin && bin.Operator == "∧")
            {
                set.UnionWith(GetLiterals(bin.Left, precedence));
                set.UnionWith(GetLiterals(bin.Right, precedence));
            }
            else
            {
                set.Add(node.ToString(precedence));
            }
            return set;
        }

        // Метод удаляет дубликаты членов ДНФ по одному за раз
        public bool ApplyRemoveDuplicateOnce()
        {
            ClearChanged();
            var ast = BuildAstFromRpn();
            var (newAst, applied) = ApplyRemoveDuplicateInAstOnce(ast);
            if (applied)
            {
                _rpn = AstToRpn(newAst);
                _changed = true;
            }
            return _changed;
        }

        private (ExpressionNode, bool) ApplyRemoveDuplicateInAstOnce(ExpressionNode node)
        {
            var disjuncts = GetDisjuncts(node);
            var indexForRemove = -1;
            for (int i = 0; i < disjuncts.Count; i++)
            {
                for (int j = i + 1; j < disjuncts.Count; j++)
                {
                    var d1 = disjuncts[i];
                    var d2 = disjuncts[j];
                    if (NodesEqual(d1, d2))
                    {
                        indexForRemove = j;
                        break;
                    }
                }
            }
            if (indexForRemove == -1 || disjuncts.Count <= 0)
            {
                return (node, false);
            }

            var resNode = disjuncts[0];
            for (int i = 1; i < disjuncts.Count; i++)
            {
                if (i == indexForRemove)
                {
                    continue;
                }
                resNode = new BinaryNode("∨", resNode, disjuncts[i]);
            }
            return (resNode, true);
        }

        // SortConjuncts сортирует конъюнкты в выражении.
        //
        // Например: b -a  +  adc     ->   -a b  +  acd
        public bool SortConjuncts() {
            ClearChanged();
            var ast = BuildAstFromRpn();
            var (newAst, applied) = SortConjuncts(ast);
            if (applied)
            {
                _rpn = AstToRpn(newAst);
                _changed = true;
            }
            return _changed;
        }

        private (ExpressionNode, bool) SortConjuncts(ExpressionNode node)
        {
            if (!(node is BinaryNode) && !(node is UnaryNode))
            {
                return (node.Clone(), false);
            }

            if (node is UnaryNode una)
            {
                var (newNode, applied) = SortConjuncts(una.Operand);
                return (new UnaryNode("¬", newNode), applied);
            }

            var bin = (BinaryNode)node;
            if (bin.Operator != "∧")
            {
                var (newLeft, appliedLeft) = SortConjuncts(bin.Left);
                var (newRight, appliedRight) = SortConjuncts(bin.Right);
                return (new BinaryNode(bin.Operator, newLeft, newRight), appliedLeft || appliedRight);
            }

            // node --- является конъюнкцией

            // Получили конъюнкты
            var conjuncts = GetConjuncts(node);
            var strBeforeSort = conjuncts.Aggregate("", (a, b) => $"{a};{b.ToString(Parser.OperatorsPrecedence)}");

            // Сортируем конъюнкты: d b -c -a    ---->     -a b -c d
            conjuncts = conjuncts.OrderBy(node =>
            {
                if (!(node is ConstantNode) && !(node is VariableNode) && !(node is UnaryNode))
                {
                    return "";
                }
                if (node is ConstantNode constNode)
                {
                    return constNode.Value ? "1" : "0";
                }
                if (node is VariableNode varNode)
                {
                    return varNode.Name;
                }
                var una = (UnaryNode)node;
                var unaOper = una.Operand;
                if (!(unaOper is VariableNode))
                {
                    return "";
                }
                return ((VariableNode)unaOper).Name;
            }).ToList();

            // Перестраиваем эту часть дерева
            var rebuilt = conjuncts[0];
            for (int k = 1; k < conjuncts.Count; k++) {
                rebuilt = new BinaryNode("∧", rebuilt, conjuncts[k]);
            }

            var strAfterSort = conjuncts.Aggregate("", (a, b) => $"{a};{b}");

            return (rebuilt, strBeforeSort != strAfterSort);
        }

        // RegenerateVariables перезадаёт _variablesInOrder,
        // на основе данных из ОПЗ, отсортировав переменные в алфавитном порядке.
        public void RegenerateVariables()
        {
            _variablesInOrder = new List<string>();
            foreach (var token in _rpn)
            {
                if (token.Type == TokenType.Variable)
                {
                    if (!_variablesInOrder.Contains(token.Value))
                    {
                        _variablesInOrder.Add(token.Value);
                    }
                }
            }
            _variablesInOrder.Sort();
        }

        // MakePDNFFromDNF создаёт СДНФ (Perfect Disjuntive Normal Form)
        // из ДНФ.
        public void MakePDNFFromDNF()
        {
            var ast = BuildAstFromRpn();
            if (!IsDNF(ast))
            {
                throw new Exception("Formula is not in DNF =(");
            }

            // Специальный случай: если весь AST — константа, ничего не делать
            if (ast is ConstantNode)
            {
                _changed = false;
                return;
            }

            var disjuncts = GetDisjuncts(ast);

            var multipleDisjuncts = new List<ExpressionNode>();

            foreach (var disjunct in disjuncts)
            {
                // Если дизъюнкт — константа
                if (disjunct is ConstantNode dConst)
                {
                    if (dConst.Value)
                    {
                        // Если 1: генерируем все минтермы (полный PDNF для 1)
                        var allMinterms = GenerateAllMinterms(_variablesInOrder);
                        multipleDisjuncts.AddRange(allMinterms);
                    }
                    // Если 0: пропускаем (не добавляем ничего)
                    continue;
                }

                var conjuncts = GetConjuncts(disjunct);
                HashSet<string> varsInDisjunct = new HashSet<string>(
                    conjuncts
                        .Select(c => c is VariableNode varNode ? varNode.Name :
                            c is UnaryNode una && una.Operand is VariableNode varNode2 ? varNode2.Name : "")
                        .Where(s => s != "")
                        .ToList()
                    );

                HashSet<string> additionalVarsSet = new HashSet<string>(_variablesInOrder);
                additionalVarsSet.ExceptWith(varsInDisjunct);
                List<string> additionalVars = additionalVarsSet.OrderBy(x => x).ToList();

                for (int i = 0; i < 1 << additionalVars.Count; i++)
                {
                    var additionalConjuncts = new List<ExpressionNode>();
                    for (int j = 0; j < additionalVars.Count; j++)
                    {
                        bool isPositive = ((i >> (additionalVars.Count - 1 - j)) & 1) == 1;
                        var varNode = new VariableNode(additionalVars[j]);
                        ExpressionNode conjunct = isPositive ? varNode : new UnaryNode("¬", varNode);
                        additionalConjuncts.Add(conjunct);
                    }

                    var newDisjunct = disjunct.Clone();
                    foreach (var conj in additionalConjuncts)
                    {
                        newDisjunct = new BinaryNode("∧", newDisjunct, conj);
                    }

                    multipleDisjuncts.Add(newDisjunct);
                }
            }

            ExpressionNode newAst;
            if (multipleDisjuncts.Count == 0)
            {
                // Если ничего не добавлено (все дизъюнкты были 0), устанавливаем 0
                newAst = new ConstantNode(false);
            }
            else
            {
                newAst = CombineDisjuncts(multipleDisjuncts);
            }

            _rpn = AstToRpn(newAst);
            _changed = true;
        }

        private List<ExpressionNode> GenerateAllMinterms(List<string> variables)
        {
            var minterms = new List<ExpressionNode>();
            int numVars = variables.Count;
            for (int i = 0; i < 1 << numVars; i++)
            {
                var conjuncts = new List<ExpressionNode>();
                for (int j = 0; j < numVars; j++)
                {
                    bool isPositive = ((i >> (numVars - 1 - j)) & 1) == 1;
                    var varNode = new VariableNode(variables[j]);
                    ExpressionNode lit = isPositive ? varNode : new UnaryNode("¬", varNode);
                    conjuncts.Add(lit);
                }
                minterms.Add(CombineConjuncts(conjuncts));
            }
            return minterms;
        }

        private bool IsDNF(ExpressionNode node)
        {
            var disjuncts = GetDisjuncts(node);
            foreach (var disjunct in disjuncts)
            {
                if (!IsConjunctionOfVars(disjunct))
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsConjunctionOfVars(ExpressionNode node)
        {
            var conjuncts = GetConjuncts(node);

            // Специальный случай для одиночной константы (0 или 1) — валидно для DNF/PDNF
            if (conjuncts.Count == 1 && conjuncts[0] is ConstantNode)
            {
                return true;
            }

            // Удаляем 1-цы (нейтральные элементы)
            conjuncts = conjuncts.Where(c => !(c is ConstantNode cc && cc.Value)).ToList();

            // Если есть 0, невалидно (должно быть упрощено раньше, но на всякий случай)
            if (conjuncts.Any(c => c is ConstantNode cc && !cc.Value))
            {
                return false;
            }

            // Если после удаления 1-ц осталось пусто — была конъюнкция 1-иц, валидно
            if (conjuncts.Count == 0)
            {
                return true;
            }

            // Проверяем оставшиеся: только литералы (var или ¬var)
            foreach (var conjunct in conjuncts)
            {
                if (conjunct is ConstantNode || conjunct is BinaryNode)
                {
                    return false;
                }
                if (conjunct is UnaryNode una && !(una.Operand is VariableNode))
                {
                    return false;
                }
            }
            return true;
        }

        // ChangeNegativesToXorsInPDNF заменяет negative literals (-a, -b, ...) в СДНФ
        // на операцию с суммой по модулю 2 (1+a, 1+b, ...)
        public void ChangeNegativesToXorsInPDNF()
        {
            var ast = BuildAstFromRpn();
            if (!IsDNF(ast))
            {
                throw new Exception("Formula is not in DNF =(");
            }

            // Оставляем (для 0/1 в ANF)
            if (ast is ConstantNode)
            {
                return;
            }

            var disjuncts = GetDisjuncts(ast);

            var newDisjuncts = new List<ExpressionNode>();

            foreach (var disjunct in disjuncts)
            {
                if (disjunct is ConstantNode dConst)
                {
                    // 1 или 0 как есть
                    newDisjuncts.Add(dConst);
                    continue;
                }

                var conjuncts = GetConjuncts(disjunct);
                var newConjuncts = new List<ExpressionNode>();

                bool hasZero = false;
                for (int i = 0; i < conjuncts.Count; i++)
                {
                    var conj = conjuncts[i];
                    if (conj is ConstantNode constNode)
                    {
                        if (!constNode.Value)
                        {
                            // Если 0 в конъюнкте, весь = 0
                            hasZero = true;
                            break;
                        }
                        // Игнор 1
                        continue;
                    }
                    var newConj = conj;
                    if (conj is UnaryNode una)
                    {
                        newConj = new BinaryNode("⊕", new ConstantNode(true), una.Operand);
                    }
                    newConjuncts.Add(newConj);
                }

                if (hasZero)
                {
                    // Пропуск дизъюнкта = 0
                    continue;
                }

                newDisjuncts.Add(CombineConjuncts(newConjuncts));
            }

            var newExpr = newDisjuncts.Count == 0 ? new ConstantNode(false) : CombineWith(newDisjuncts, "⊕");
            _rpn = AstToRpn(newExpr);
        }

        // ExtractExpressions рекурсивно извлекает операнды операций наподобие
        // a ^ b ^ c  --->   [a, b, c].
        //
        // Данный метод имеет смысл для операций, для которых работает
        // ассоциативность (конъюнкция, дизъюнкция, сумма под модулю 2).
        private List<ExpressionNode> ExtractExpressions(ExpressionNode node, string @operator)
        {
            var list = new List<ExpressionNode>();
            if (node is BinaryNode bin && bin.Operator == @operator)
            {
                list.AddRange(ExtractExpressions(bin.Left, @operator));
                list.AddRange(ExtractExpressions(bin.Right, @operator));
            }
            else
            {
                list.Add(node);
            }
            return list;
        }

        private List<ExpressionNode> GetConjuncts(ExpressionNode node)
        {
            return ExtractExpressions(node, "∧");
        }

        private List<ExpressionNode> GetDisjuncts(ExpressionNode node)
        {
            return ExtractExpressions(node, "∨");
        }

        private ExpressionNode CombineWith(List<ExpressionNode> expressions, string @operator)
        {
            if (expressions.Count <= 0)
            {
                return new ConstantNode(false);
            }
            var result = expressions[0];
            for (int i = 1; i < expressions.Count; i++)
            {
                result = new BinaryNode(@operator, result, expressions[i]);
            }
            return result;
        }

        // CombineConjuncts объединяет конъюнкты conjuncts операцией конъюнкции
        private ExpressionNode CombineConjuncts(List<ExpressionNode> conjuncts)
        {
            return CombineWith(conjuncts, "∧");
        }

        // CombineDisjuncts объединяет дизъюнкты disjuncts операцией дизъюнкции
        private ExpressionNode CombineDisjuncts(List<ExpressionNode> disjuncts)
        {
            return CombineWith(disjuncts, "∨");
        }

        public bool ExpandXorBrackets()
        {
            /*
            bcdf * 1   + bcdf * a

            (f + fa) * b * c * d
            (fb + fab) * c * d
            (fbc + fabc) * d
            fbcd + fabcd

            Конъюнкт:
            1. Простой: переменная или константа
            2. Сложный: + (сумма по модулю 2)

            Если сложных нет, то действие не требуется.

            Если есть хотя бы 1 сложный, то:
            1. Объединяем простые в единую конъюнкцию: fbcd
            2. Выбираем любой 1 сложный. Отделяем от остальных сложных.
            3. Если есть хотя бы 1 простой, то:
	            3.1. Выполняем перемножение простого со сложным (поэлементное) с упрощением на месте.
            4. Иначе:
	            4.1. Выбрать второй сложный.
            */

            var ast = BuildAstFromRpn();

            // a xor b xor c ---> [a, b, c]
            var terms = ExtractExpressions(ast, "⊕");
            var newTerms = new List<ExpressionNode>();

            bool wasChanged = false;

            foreach (var term in terms)
            {
                var conjuncts = GetConjuncts(term);
                // Допустим, что имеем следующее выражение: b*c*(1+d+b)*c*(f+k+d)*g*h
                // Тогда conjuncts: b, c, (1+d+b), c, (f+k+d), g, h
                //
                // Распределим на простые и сложные в терминах, определённых выше
                var simple = new List<ExpressionNode>();
                var complex = new List<ExpressionNode>();
                foreach (var conj in conjuncts)
                {
                    if (conj is BinaryNode bin && bin.Operator == "⊕")
                    {
                        complex.Add(conj);
                    } else
                    {
                        simple.Add(conj);
                    }
                }
                // Нет сложных -> скобки раскрыты
                if (wasChanged || complex.Count <= 0)
                {
                    newTerms.Add(term);
                    continue;
                }
                // Иначе:
                if (simple.Count > 0)
                {
                    // Если простые есть, то перемножаем простые с 1-м сложным
                    var oneComplex = complex[0];
                    var complexTerms = ExtractExpressions(oneComplex, "⊕");
                    var newTermsInside = new List<ExpressionNode>();
                    foreach (var complexTerm in complexTerms)
                    {
                        var termInside = new List<ExpressionNode>(simple);
                        termInside.Add(complexTerm);
                        newTermsInside.Add(CombineConjuncts(termInside));
                    }
                    var newOneComplex = CombineWith(newTermsInside, "⊕");
                    var newExpression = new List<ExpressionNode>();
                    newExpression.Add(newOneComplex);
                    newExpression.AddRange(complex.Skip(1).ToList());
                    newTerms.Add(CombineConjuncts(newExpression));
                    wasChanged = true;
                } else
                {
                    if (complex.Count < 2) {
                        newTerms.Add(term);
                    } else
                    {
                        var firstComplex = complex[0];
                        var secondComplex = complex[1];
                        var firstComplexTerms = ExtractExpressions(firstComplex, "⊕");
                        var secondComplexTerms = ExtractExpressions(secondComplex, "⊕");
                        var newComplex = new List<ExpressionNode>();
                        foreach (var term1 in firstComplexTerms)
                        {
                            foreach (var term2 in secondComplexTerms)
                            {
                                newComplex.Add(new BinaryNode("∧", term1, term2));
                            }
                        }
                        var newExpression = new List<ExpressionNode>();
                        newExpression.Add(CombineWith(newComplex, "⊕"));
                        newExpression.AddRange(complex.Skip(2).ToList());
                        newTerms.Add(CombineConjuncts(newExpression));
                        wasChanged = true;
                    }
                }
            }

            if (wasChanged)
            {
                _rpn = AstToRpn(CombineWith(newTerms, "⊕"));
            }

            return wasChanged;
        }

        // SimplifyConjuncts заменяет конъюнкции вида 1a1dc1111 на acd
        public bool SimplifyConjuncts()
        {
            var ast = BuildAstFromRpn();
            var (resAst, wasChanged) = SimplifyConjuncts(ast);

            if (wasChanged)
            {
                _rpn = AstToRpn(resAst);
            }
            return wasChanged;
        }

        private (ExpressionNode, bool) SimplifyConjuncts(ExpressionNode node)
        {
            if (node is BinaryNode bin)
            {
                if (bin.Operator == "∧")
                {
                    var conjuncts = GetConjuncts(bin);
                    // Если нет единиц в составе конъюнкции (или единица всего лишь одна), то распространяемся дальше
                    if (!conjuncts.Any(x => x is ConstantNode con && con.Value) || conjuncts.Count == 1)
                    {
                        var (left, leftChanged) = SimplifyConjuncts(bin.Left);
                        var (right, rightChanged) = SimplifyConjuncts(bin.Right);
                        return (new BinaryNode(bin.Operator, left, right), leftChanged || rightChanged);
                    }
                    // Иначе, убираем единицы.
                    var newConjuncts = conjuncts.Where(x => !(x is ConstantNode con) || !con.Value).ToList();
                    if (newConjuncts.Count <= 0)
                    {
                        // Если была конъюнкция единиц (1^1^1), то оставляем 1 единицу.
                        newConjuncts.Add(new ConstantNode(true));
                    }
                    var resElem = CombineConjuncts(newConjuncts);
                    return (resElem, true);
                }
                else
                {
                    var (left, leftChanged) = SimplifyConjuncts(bin.Left);
                    var (right, rightChanged) = SimplifyConjuncts(bin.Right);
                    return (new BinaryNode(bin.Operator, left, right), leftChanged || rightChanged);
                }
            }
            else if (node is UnaryNode una)
            {
                var (res, changed) = SimplifyConjuncts(una.Operand);
                return (res, changed);
            }
            else
            {
                return (node, false);
            }
        }

        public bool RemovePairInXor()
        {
            var ast = BuildAstFromRpn();
            bool wasChanged = false;

            var terms = ExtractExpressions(ast, "⊕");
            // a + b + c + d + e
            //
            // (a, b), (a, c), (a, d), (a, e)
            // (b, c), (b, d), (b, e)
            // (c, d), (c, e)
            // (d, e)
            //  ^
            for (int i = 0; i < terms.Count - 1; i++)
            {
                var termA = terms[i];
                for (int j = i + 1; j < terms.Count; j++)
                {
                    var termB = terms[j];
                    if (NodesEqual(termA, termB))
                    {
                        wasChanged = true;
                        terms.RemoveAt(j);
                        terms.RemoveAt(i);
                        break;
                    }
                }
                if (wasChanged)
                {
                    break;
                }
            }

            if (wasChanged)
            {
                _rpn = AstToRpn(CombineWith(terms, "⊕"));
            }
            return wasChanged;
        }

        // Возвращает максимальное число операндов конъюнкции,
        // учитывая все конъюнкции, любого выражения.
        public int GetMaxOperandsInConjunctions()
        {
            var ast = BuildAstFromRpn();
            return GetMaxOperandsInConjunctions(ast);
        }

        private int GetMaxOperandsInConjunctions(ExpressionNode node)
        {
            if (node is BinaryNode bin)
            {
                if (bin.Operator == "∧")
                {
                    var conjs = GetConjuncts(bin);
                    int max = 0;
                    foreach (var conj in conjs)
                    {
                        int cur = GetMaxOperandsInConjunctions(conj);
                        if (cur > max) {
                            max = cur;
                        }
                    }
                    if (conjs.Count > max)
                    {
                        max = conjs.Count;
                    }
                    return max;
                }
                int maxLeft = GetMaxOperandsInConjunctions(bin.Left);
                int maxRight = GetMaxOperandsInConjunctions(bin.Right);
                return maxLeft > maxRight ? maxLeft : maxRight;
            }
            if (node is UnaryNode una)
            {
                return GetMaxOperandsInConjunctions(una.Operand);
            }
            return 0;
        }

        public bool ApplyConstantsSimplificationOnce()
        {
            ClearChanged();
            var ast = BuildAstFromRpn();
            var (newAst, applied) = ApplyConstantsSimplificationInAstOnce(ast);
            if (applied)
            {
                _rpn = AstToRpn(newAst);
                _changed = true;
            }
            return _changed;
        }

        private (ExpressionNode, bool) ApplyConstantsSimplificationInAstOnce(ExpressionNode node)
        {
            if (node is ConstantNode || node is VariableNode)
                return (node, false);

            if (node is UnaryNode unary)
            {
                var (newOperand, applied) = ApplyConstantsSimplificationInAstOnce(unary.Operand);
                if (applied)
                {
                    unary.Operand = newOperand;
                    return (unary, true);
                }
                return (unary, false);
            }

            if (node is BinaryNode binary)
            {
                // Рекурсия
                var (newLeft, appliedLeft) = ApplyConstantsSimplificationInAstOnce(binary.Left);
                binary.Left = newLeft;
                var (newRight, appliedRight) = ApplyConstantsSimplificationInAstOnce(binary.Right);
                binary.Right = newRight;
                bool applied = appliedLeft || appliedRight;

                if (binary.Operator == "∧")
                {
                    // Если left или right — 0, весь ∧ = 0
                    if ((binary.Left is ConstantNode leftConst && !leftConst.Value) ||
                        (binary.Right is ConstantNode rightConst && !rightConst.Value))
                    {
                        return (new ConstantNode(false), true);
                    }
                    // Если left — 1, вернуть right
                    if (binary.Left is ConstantNode leftConst1 && leftConst1.Value)
                    {
                        return (binary.Right, true);
                    }
                    // Если right — 1, вернуть left
                    if (binary.Right is ConstantNode rightConst1 && rightConst1.Value)
                    {
                        return (binary.Left, true);
                    }
                }
                else if (binary.Operator == "∨")
                {
                    // Если left или right — 1, весь ∨ = 1
                    if ((binary.Left is ConstantNode leftConst && leftConst.Value) ||
                        (binary.Right is ConstantNode rightConst && rightConst.Value))
                    {
                        return (new ConstantNode(true), true);
                    }
                    // Если left — 0, вернуть right
                    if (binary.Left is ConstantNode leftConst0 && !leftConst0.Value)
                    {
                        return (binary.Right, true);
                    }
                    // Если right — 0, вернуть left
                    if (binary.Right is ConstantNode rightConst0 && !rightConst0.Value)
                    {
                        return (binary.Left, true);
                    }
                }

                return (binary, applied);
            }

            return (node, false);
        }
    }
}
