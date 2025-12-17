namespace BooleanCompletenessBack.Models
{
    public class KLResultPascalTriangleMethod : BaseKResult
    {
        // Хранит треугольник Паскаля (полученный применением
        // операций "сумма по модулю 2").
        //
        // Будут иметь смысл только элементы выше главной диагонали,
        // т.к. так фронтенду будет проще сопоставлять индексы
        // с внешним видом треугольника. Именно поэтому не используем
        // jagged array.
        public int[,] Triangle { get; set; }

        public Monomial[] ZhegalkinPolynomial { get; set; }
    }
}
