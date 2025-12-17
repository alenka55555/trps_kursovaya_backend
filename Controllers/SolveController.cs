using BooleanCompletenessBack.BusinessLogic;
using BooleanCompletenessBack.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;

namespace BooleanCompletenessBack.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SolveController : ControllerBase
    {
        [HttpPost]
        public IActionResult Solve([FromBody] SolveInput inp)
        {
            int paramsCount = inp.ParamNames.Length;
            if (paramsCount > 5)
            {
                throw new ClientException("Число параметров не должно быть больше 5");
            }
            if (inp.Fs != null && inp.Fs.Length > 5)
            {
                throw new ClientException("Число функций не должно быть больше 5");
            }

            if (inp.FsFormulas == null)
            {
                Console.WriteLine("FsFormulas == null");
            } else
            {
                Console.WriteLine($"FsFormulas: {string.Join(", ", inp.FsFormulas)}");
            }

            Console.WriteLine($"ParamsCount: {paramsCount}");

            string fs = string.Join(",", inp.Fs[0]);
            Console.WriteLine($"f1: {fs}");

            try
            {
                inp.Validate();
            } catch (Exception ex)
            {
                return BadRequest(new {
                    errorMsg = ex.Message,
                });
            }


            var results = new SolverResult[inp.Fs.Length];
            int i = 0;

            for (int j = 0; j < inp.Fs.Length; j++)
            {
                var funcTruthTable = inp.Fs[j];
                var funcFormula = inp.FsFormulas == null ? "" : inp.FsFormulas[j];

                var k0 = new K0Solver(inp.ParamNames, funcTruthTable).Solve();
                var k1 = new K1Solver(inp.ParamNames, funcTruthTable).Solve();
                var km = new KMSolver(inp.ParamNames, funcTruthTable, inp.ReduceOutput).Solve();
                var ks = new KSSolver(inp.ParamNames, funcTruthTable, inp.ReduceOutput).Solve();
                var kl = new KLSolver(inp.ParamNames, funcTruthTable, funcFormula).Solve();
                results[i] = new SolverResult
                {
                    K0Result = k0,
                    K1Result = k1,
                    KMResult = km,
                    KSResult = ks,
                    KLResult = kl,
                };
                i++;
            }

            bool hasAtLeast1FuncNotBelongToK0 = false;
            bool hasAtLeast1FuncNotBelongToK1 = false;
            bool hasAtLeast1FuncNotBelongToKM = false;
            bool hasAtLeast1FuncNotBelongToKS = false;
            bool hasAtLeast1FuncNotBelongToKL = false;
            foreach (var res in results)
            {
                if (!res.K0Result.BelongsToClass)
                {
                    hasAtLeast1FuncNotBelongToK0 = true;
                    break;
                }
            }
            foreach (var res in results)
            {
                if (!res.K1Result.BelongsToClass)
                {
                    hasAtLeast1FuncNotBelongToK1 = true;
                    break;
                }
            }
            foreach (var res in results)
            {
                if (!res.KMResult.BelongsToClass)
                {
                    hasAtLeast1FuncNotBelongToKM = true;
                    break;
                }
            }
            foreach (var res in results)
            {
                if (!res.KSResult.BelongsToClass)
                {
                    hasAtLeast1FuncNotBelongToKS = true;
                    break;
                }
            }
            foreach (var res in results)
            {
                if (!res.KLResult.TriangleMethod.BelongsToClass)
                {
                    hasAtLeast1FuncNotBelongToKL = true;
                    break;
                }
            }

            bool systemIsComplete = hasAtLeast1FuncNotBelongToK0 && hasAtLeast1FuncNotBelongToK1 &&
                hasAtLeast1FuncNotBelongToKM && hasAtLeast1FuncNotBelongToKS && hasAtLeast1FuncNotBelongToKL;

            return Ok(new
            {
                xsCount = paramsCount,
                fsCount = inp.Fs.Length,
                paramNames = inp.ParamNames,
                results = results,
                systemIsComplete = systemIsComplete,
                hasAtLeast1FuncNotBelongToK0 = hasAtLeast1FuncNotBelongToK0,
                hasAtLeast1FuncNotBelongToK1 = hasAtLeast1FuncNotBelongToK1,
                hasAtLeast1FuncNotBelongToKM = hasAtLeast1FuncNotBelongToKM,
                hasAtLeast1FuncNotBelongToKS = hasAtLeast1FuncNotBelongToKS,
                hasAtLeast1FuncNotBelongToKL = hasAtLeast1FuncNotBelongToKL,
            });
        }
    }
}
