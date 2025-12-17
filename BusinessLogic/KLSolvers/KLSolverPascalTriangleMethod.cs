using BooleanCompletenessBack.Models;

namespace BooleanCompletenessBack.BusinessLogic.KLSolvers
{
    public class KLSolverPascalTriangleMethod : BaseSolver<KLResultPascalTriangleMethod>
    {
        public KLSolverPascalTriangleMethod(string[] paramNames, int[] funcValues) : base(paramNames, funcValues)
        {
            /* ничего */
        }

        public override KLResultPascalTriangleMethod Solve()
        {
            // СТРОИМ ТРЕУГОЛЬНИК ПАСКАЛЯ (mod 2)

            int fCnt = _funcValues.Length;
            var triangle = new int[fCnt, fCnt];
            // Заполняем первую строку (строку с индексом 0)
            for (int i = 0; i < fCnt; i++)
            {
                triangle[0, i] = _funcValues[i];
            }
            // Начинаем со строки с индексом 1
            for (int i = 1; i < fCnt; i++)
            {
                for (int j = i; j < fCnt; j++)
                {
                    triangle[i, j] = triangle[i - 1, j - 1] ^ triangle[i - 1, j];
                }
            }

            // ФОРМИРУЕМ ПОЛИНОМ ЖЕГАЛКИНА

            var gen = new ParamsGenerator(_paramsCount);
            int[][] paramsAr = gen.Generate();
            var polynomial = new Monomial[fCnt];
            for (int i = 0; i < fCnt; i++)
            {
                var @params = paramsAr[i];
                var value = GenerateValueByParams(@params);
                polynomial[i] = new Monomial
                {
                    ParamsIndices = @params,
                    Present = triangle[i, i] == 1,
                    Value = value,
                };
            }

            bool hasMonomialWithMultipleX = false;
            foreach (var monomial in polynomial)
            {
                if (monomial.Present)
                {
                    // monomial.ParamsIndices = [1, 0, 1];
                    // countOfOnes = 2;

                    // Aggregate - аналог reduce из js
                    int countOfOnes = monomial.ParamsIndices.Aggregate(0, (acc, x) => acc + x);
                    if (countOfOnes > 1)
                    {
                        hasMonomialWithMultipleX = true;
                        break;
                    }
                }
            }

            return new KLResultPascalTriangleMethod
            {
                Triangle = triangle,
                ZhegalkinPolynomial = polynomial,
                BelongsToClass = !hasMonomialWithMultipleX,
            };
        }

        private string GenerateValueByParams(int[] @params)
        {
            var value = "";
            for (int j = 0; j < _paramsCount; j++)
            {
                if (@params[j] == 1)
                {
                    value += _paramNames[j];
                }
            }
            if (value == "")
            {
                value = "1";
            }
            return value;
        }
    }
}
