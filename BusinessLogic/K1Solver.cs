using BooleanCompletenessBack.Models;

namespace BooleanCompletenessBack.BusinessLogic
{
    public class K1Solver : BaseSolver<K1Result>
    {
        public K1Solver(string[] paramNames, int[] funcValues) : base(paramNames, funcValues)
        {
            /* ничего */
        }

        public override K1Result Solve()
        {
            var valueOnOnes = _funcValues[_funcValues.Length - 1];
            return new K1Result
            {
                BelongsToClass = valueOnOnes == 1,
                ValueOnOnes = valueOnOnes,
            };
        }
    }
}
