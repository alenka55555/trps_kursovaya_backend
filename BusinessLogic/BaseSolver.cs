using BooleanCompletenessBack.Models;

namespace BooleanCompletenessBack.BusinessLogic
{
    public abstract class BaseSolver<TResult>
    {
        protected int _paramsCount;
        protected int[] _funcValues;
        protected string[] _paramNames;

        public BaseSolver(string[] paramNames, int[] funcValues)
        {
            if (funcValues.Length < 1)
            {
                throw new Exception("funcValues.Length should be >= 1");
            }
            if ((int)Math.Pow(2, paramNames.Length) != funcValues.Length)
            {
                throw new Exception("2**paramsCount should be equal to funcValues.Length");
            }
            this._paramsCount = paramNames.Length;
            this._paramNames = paramNames;
            this._funcValues = funcValues;
        }

        public abstract TResult Solve();
    }
}
