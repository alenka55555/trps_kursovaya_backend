using BooleanCompletenessBack.BusinessLogic.KLSolvers;
using BooleanCompletenessBack.Models;

namespace BooleanCompletenessBack.BusinessLogic
{
    public class KLSolver : BaseSolver<KLResult>
    {
        private KLSolverPascalTriangleMethod _pascalMethodSolver;
        private KLSolverAnalyticalMethod _analyticalMethodSolver;

        public KLSolver(string[] paramNames, int[] funcValues, string formula) : base(paramNames, funcValues)
        {
            _pascalMethodSolver = new KLSolverPascalTriangleMethod(paramNames, funcValues);
            _analyticalMethodSolver = formula == "" ? null : new KLSolverAnalyticalMethod(formula);
        }

        public override KLResult Solve()
        {
            return new KLResult
            {
                TriangleMethod = _pascalMethodSolver.Solve(),
                AnalyticalMethod = _analyticalMethodSolver?.Solve(),
            };
        }
    }
}
