using BooleanCompletenessBack.Models;

namespace BooleanCompletenessBack.BusinessLogic
{
    public class K0Solver : BaseSolver<K0Result>
    {
        public K0Solver(string[] paramNames, int[] funcValues) : base(paramNames, funcValues)
        {
            /* ничего */
        }

        public override K0Result Solve()
        {
            return new K0Result
            {
                BelongsToClass = _funcValues[0] == 0,
                ValueOnZeros = _funcValues[0],
            };
        }
    }
}
