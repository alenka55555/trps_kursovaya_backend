namespace BooleanCompletenessBack.BusinessLogic
{
    public class ParamsGenerator
    {
        private int _count;

        public ParamsGenerator(int count)
        {
            this._count = count;
        }

        // [[0 0 0], [0 0 1], [0 1 0], [0 1 1], ...]
        public int[][] Generate()
        {
            int cnt = (int)Math.Pow(2, _count);

            int[][] res = new int[cnt][];

            int[] current = new int[_count];
            for (int i = 0; i < _count; i++)
            {
                current[i] = 0;
            }
            for (int i = 0; i < cnt; i++)
            {
                res[i] = current;
                current = Next(current);
            }
            return res;
        }

        private int[] Next(int[] current)
        {
            // current = [0 0 1]
            int[] result = new int[current.Length];
            for (int i = 0; i < current.Length; i++)
            {
                result[i] = current[i];
            }
            int zeroIndex = result.Length - 1;
            while (zeroIndex >= 0 && result[zeroIndex] == 1)
            {
                zeroIndex--;
            }
            if (zeroIndex < 0)
            {
                return null;
            }
            result[zeroIndex] = 1;
            for (int i = zeroIndex + 1; i < result.Length; i++)
            {
                result[i] = 0;
            }
            return result;
        }
    }
}
