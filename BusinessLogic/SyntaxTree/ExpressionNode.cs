namespace BooleanCompletenessBack.BusinessLogic.SyntaxTree
{
    public abstract class ExpressionNode
    {
        // Преобразовать поддерево в katex-строку.
        //
        // precedence - приоритет операций (нужен для отображения с учетом приоритетов)
        public abstract string ToString(Dictionary<string, int> precedence);

        // Сбор переменных
        public abstract List<string> GetVariables(); 

        public abstract ExpressionNode Clone();
    }
}
