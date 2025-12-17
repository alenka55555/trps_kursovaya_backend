namespace BooleanCompletenessBack.BusinessLogic.FormulaParser
{
    public class VarsCombiner
    {
        private List<BooleanExpression> _exprs;

        public VarsCombiner(List<BooleanExpression> exprs)
        {
            this._exprs = exprs;
        }

        public List<string> Combine()
        {
            if (_exprs.Count <= 0)
            {
                return new List<string>();
            }
            var vars = _exprs[0].GetVariables();
            for (int i = 1; i < _exprs.Count; i++)
            {
                var expr = _exprs[i];
                var newVars = expr.GetVariables();
                foreach (var v in newVars)
                {
                    if (!vars.Contains(v))
                    {
                        vars.Add(v);
                    }
                }
            }
            return vars;
        }
    }
}
