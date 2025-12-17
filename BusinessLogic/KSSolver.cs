using BooleanCompletenessBack.Models;

namespace BooleanCompletenessBack.BusinessLogic
{
    public class KSSolver : BaseSolver<KSResult>
    {
        private bool _reduceOutput;

        public KSSolver(string[] paramNames, int[] funcValues, bool reduceOutput) : base(paramNames, funcValues)
        {
            this._reduceOutput = reduceOutput;
        }

        public override KSResult Solve()
        {
            var gen = new ParamsGenerator(_paramsCount);
            int[][] paramsAr = gen.Generate();
            List<KSStatement> pairs = new List<KSStatement>();
            bool belongs = true;
            int i = 0;
            while (i < paramsAr.Length / 2 && (!_reduceOutput || _reduceOutput && belongs))
            {
                int j = paramsAr.Length - i - 1;

                bool opposite = _funcValues[i] != _funcValues[j];
                pairs.Add(new KSStatement
                {
                    Sigma1 = paramsAr[i],
                    Sigma2 = paramsAr[j],
                    F1IsOppositeToF2 = opposite,
                });
                if (!opposite)
                {
                    belongs = false;
                }

                i++;
            }
            if (belongs && _reduceOutput)
            {
                pairs = pairs.Take(5).ToList();
            }
            return new KSResult
            {
                BelongsToClass = belongs,
                Statements = pairs.ToArray(),
            };
        }
    }
}
