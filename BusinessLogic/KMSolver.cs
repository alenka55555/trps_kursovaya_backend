using BooleanCompletenessBack.Models;

namespace BooleanCompletenessBack.BusinessLogic
{
    public class KMSolver : BaseSolver<KMResult>
    {
        private bool _reduceOutput;

        public KMSolver(string[] paramNames, int[] funcValues, bool reduceOutput) : base(paramNames, funcValues)
        {
            this._reduceOutput = reduceOutput;
        }

        public override KMResult Solve()
        {
            var gen = new ParamsGenerator(_paramsCount);
            int[][] paramsAr = gen.Generate();
            List<KMStatement> pairs = new List<KMStatement>();
            bool belongs = true;
            int i = 0;
            while (i < paramsAr.Length - 1 && (belongs || !_reduceOutput))
            {
                int j = i + 1;
                while (j < paramsAr.Length && (belongs || !_reduceOutput))
                {
                    if (LessThan(paramsAr[i], paramsAr[j])) {
                        bool lessOrEqual = _funcValues[i] <= _funcValues[j];
                        pairs.Add(new KMStatement
                        {
                            Sigma1 = paramsAr[i],
                            Sigma2 = paramsAr[j],
                            F1LessOrEqualF2 = lessOrEqual,
                        });
                        if (!lessOrEqual)
                        {
                            belongs = false;
                        }
                    }
                    j++;
                }
                i++;
            }
            if (belongs && _reduceOutput)
            {
                pairs = pairs.Take(5).ToList();
            }
            return new KMResult
            {
                BelongsToClass = belongs,
                Statements = pairs.ToArray(),
            };
        }

        private bool LessThan(int[] a, int[] b)
        {
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] > b[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
