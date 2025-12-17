namespace BooleanCompletenessBack.Models
{
    public class SolveInput
    {
        // Названия параметров
        public string[] ParamNames { get; set; }

        // Массив со значениями функций, наподобие:
        // [[0 0 1 0], [1 1 1 1]].
        //
        // [0 0 1 0] - это значения функции f1.
        // [1 1 1 1] - это значения функции f2.
        //
        // Функций может быть сколь угодно много.
        public int[][] Fs { get; set; }

        // null - если формулы не были введены пользователем (только таблицы истинности)
        //
        // Или же список введёных пользователем формул функций
        public List<string>? FsFormulas { get; set; }

        // Если true, то решение для классов Поста K_M и K_S
        // будет по возможности сокращено.
        public bool ReduceOutput { get; set; }

        // Validate проверяет данные на корректность
        // и выбрасывает исключение, если что-то не верно в данных.
        public void Validate()
        {
            int paramsCount = ParamNames.Length;

            if (FsFormulas != null && FsFormulas.Count != Fs.Length)
            {
                throw new Exception("Число формул (если задано) " +
                    "должно быть равно числу функций (заданных таблицами истинности)");
            }

            if (Fs.Length < 1)
            {
                throw new Exception("Число функций должно быть 1 или более");
            }
            if (paramsCount < 0 || paramsCount > 10)
            {
                throw new Exception($"Число параметров должно быть от 0 до 10. У вас {paramsCount}");
            }
            int linesCount = (int) Math.Pow(2, paramsCount);
            for (int i = 0; i < Fs.Length; i++)
            {
                if (Fs[i].Length != linesCount)
                {
                    throw new Exception($"В каждой функции должно быть {linesCount} значений," +
                        $" для {paramsCount} параметров, но это не так для функции №{i + 1}");
                }
                for (int j = 0; j < Fs[i].Length; j++)
                {
                    int v = Fs[i][j];
                    if (v < 0 || v > 1)
                    {
                        throw new Exception($"В качестве значений функций допустимы 0 и 1, но " +
                            $"в функции №{i+1} используется значение {v} {j+1}-ым по счёту");
                    }
                }
            }
        }

    }
}
