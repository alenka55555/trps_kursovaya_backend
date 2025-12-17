using BooleanCompletenessBack.BusinessLogic;
using BooleanCompletenessBack.BusinessLogic.FormulaParser;
using BooleanCompletenessBack.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace BooleanCompletenessBack.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CheckAndParseFormulasController : ControllerBase
    {

        [HttpPost]
        public IActionResult CheckAndParseFormulas([FromBody] List<string> formulas)
        {
            if (formulas != null && formulas.Count > 5)
            {
                throw new ClientException("Число функций не должно быть больше 5");
            }

            Console.WriteLine($"Formulas: {string.Join(", ", formulas)}");

            if (formulas.Count <= 0)
            {
                return Ok(new
                {
                    vars = new List<string>(),
                    truthTable = new int[0, 0],
                });
            }

            int maxVarsLimit = 5;
            var expressions = formulas.Select(f => new BooleanExpression(f, maxVarsLimit)).ToList();


            var varsCombiner = new VarsCombiner(expressions);

            // Все переменные системы булевых функций.
            // f1(a, b), f2(a, c) -> systemVars = ["a", "b", "c"]
            var systemVars = varsCombiner.Combine();
            if (systemVars.Count > maxVarsLimit)
            {
                throw new ClientException($"Общее число переменных не должно превышать {maxVarsLimit}");
            }

            if (systemVars.Count <= 0)
            {
                systemVars = new List<string>() { "a" };
            }

            // 1<<systemVars.Count <=> 2 ** systemVars.Count
            //
            // Число строк в таблице истинности.

            //   0110 << 1
            // = 1100
            //
            //   0110 << 2
            // = 1000
            //
            // a << b   <->   a * 2**b
            int rowsCount = 1 << systemVars.Count;

            // Таблица истинности будет содержать systemVars.Count для значений переменных и
            // expressions.Count столбцов для значений функций согласно формулам.
            //
            // truthTable = [
            //    a  b  c  f1 f2
            //   [0, 0, 0, ?, ?],
            //   [0, 0, 1, ?, ?],
            //   [0, 1, 0, ?, ?],
            //   ...
            // ]
            int[,] truthTable = new int[rowsCount, systemVars.Count + expressions.Count];

            // Задаём значения параметров ((0, 0, 0), (0, 0, 1), (0, 1, 0), ...)
            var expr0Table = expressions[0].GetTruthTable(systemVars);
            for (int i = 0; i < rowsCount; i++)
            {
                for (int j = 0; j < systemVars.Count; j++)
                {
                    truthTable[i, j] = expr0Table[i][j] ? 1 : 0;
                }
            }

            // Задаём значения функций
            for (int i = 0; i < expressions.Count; i++)
            {
                var expr = expressions[i];
                var exprITable = expr.GetTruthTable(systemVars);
                for (int j = 0; j < rowsCount; j++)
                {
                    truthTable[j, i + systemVars.Count] = exprITable[j][systemVars.Count] ? 1 : 0;
                }
            }

            return Ok(new
            {
                vars = systemVars,
                formulasCount = expressions.Count,
                truthTable = truthTable,
            });
        }
    }
}
