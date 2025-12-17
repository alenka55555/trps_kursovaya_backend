using BooleanCompletenessBack.BusinessLogic.FormulaParser;
using BooleanCompletenessBack.Models;

namespace BooleanCompletenessBack.BusinessLogic.KLSolvers
{
    public class KLSolverAnalyticalMethod
    {
        private string _formula;
        private BooleanExpression _expr;

        public KLSolverAnalyticalMethod(string formula)
        {
            this._formula = formula;
        }

        public KLResultAnalyticalMethod Solve()
        {
            this._expr = new BooleanExpression(_formula, maxVariablesLimit: 5);

            string sourceExpression = _expr.ToString();

            // Шаг 1. Раскрываем все операции кроме И, ИЛИ, НЕ
            var revealingComplexOperators = RevealComplexOperators();

            // Шаг 2. Преобразуем формулу в ДНФ
            var dnfRetrieving = ToDNF();

            // Шаг 3. Преобразуем ДНФ в СДНФ
            var pdnfRetrieving = ToPDNF();

            // Шаг 4. Преобразуем СДНФ в АНФ (полином Жегалкина)
            var anfRetrieving = ToANF();

            // Выясняем, есть ли хотя бы 1 моном, содержащий больше
            // 1-ой переменной.
            //
            // Если такого нет, то значит принадлежит классу KL Поста.
            bool belongs = _expr.GetMaxOperandsInConjunctions() <= 1;

            return new KLResultAnalyticalMethod
            {
                BelongsToClass = belongs,
                SourceExpression = sourceExpression,
                RevealingComplexOperators = revealingComplexOperators,
                DnfRetrieving = dnfRetrieving,
                PdnfRetrieving = pdnfRetrieving,
                AnfRetrieving = anfRetrieving,
            };
        }

        // Раскрываем сложные операции
        private List<ExpressionChange> RevealComplexOperators()
        {
            var changes = new List<ExpressionChange>();

            // антиимпликация, антиэквивалентность
            if (_expr.Replace("↛"))
            {
                changes.Add(new ExpressionChange("Раскрыли антиимпликацию", _expr.ToString()));
            }
            if (_expr.Replace("↮"))
            {
                changes.Add(new ExpressionChange("Раскрыли антиэквивалентность", _expr.ToString()));
            }

            if (_expr.Replace("↓"))
            {
                changes.Add(new ExpressionChange("Раскрыли стрелку Пирса", _expr.ToString()));
            }
            if (_expr.Replace("↑"))
            {
                changes.Add(new ExpressionChange("Раскрыли штрих Шеффера", _expr.ToString()));
            }
            if (_expr.Replace("→"))
            {
                changes.Add(new ExpressionChange("Раскрыли импликацию", _expr.ToString()));
            }
            if (_expr.Replace("↔"))
            {
                changes.Add(new ExpressionChange("Раскрыли эквивалентность", _expr.ToString()));
            }
            if (_expr.Replace("⊕"))
            {
                changes.Add(new ExpressionChange("Раскрыли сумму по модулю 2", _expr.ToString()));
            }

            return changes;
        }

        private List<ExpressionChange> ToDNF()
        {
            var changes = new List<ExpressionChange>();

            // Получаем NNF (Negation Normal Form),
            // обрабатывая двойные отрицания и
            // применяя закон де Моргана
            bool transformed = true;
            while (transformed)
            {
                transformed = false;
                // --a = a
                if (_expr.ApplyDoubleNegationOnce())
                {
                    changes.Add(new ExpressionChange("Убрали двойное отрицание", _expr.ToString()));
                    transformed = true;
                }
                // -(a ^ b) = -a v -b
                // -(a v b) = -a ^ -b
                if (_expr.ApplyDeMorganOnce())
                {
                    changes.Add(new ExpressionChange("Закон де Моргана", _expr.ToString()));
                    transformed = true;
                }
            }

            // Закон дистрибутивности
            transformed = true;
            while (transformed)
            {
                transformed = _expr.ApplyDistributiveOnce();
                if (transformed)
                {
                    changes.Add(new ExpressionChange("Закон дистрибутивности", _expr.ToString()));
                }
            }

            // Применяем упрощения (пошагово)
            transformed = true;
            while (transformed)
            {
                transformed = false;
                // 1. Idempotence:      AA = A;  A ∨ A = A
                // 2. Contradiction:    A¬A = 0; A ∧ 0 = 0; A ∨ 0 = A
                // 3. Absorption:       A ∨ (A ∧ B) = A
                // 4. Remove Duplicate: A ∨ A = A  (в слагаемых DNF)
                if (_expr.ApplyIdempotenceOnce())
                {
                    changes.Add(new ExpressionChange("Свойство идемпотентности", _expr.ToString()));
                    transformed = true;
                }
                if (_expr.ApplyContradictionOnce())
                {
                    changes.Add(new ExpressionChange("Закон противоречия/нуля", _expr.ToString()));
                    transformed = true;
                }
                if (_expr.ApplyAbsorptionOnce())
                {
                    changes.Add(new ExpressionChange("Закон поглощения", _expr.ToString()));
                    transformed = true;
                }
                if (_expr.ApplyRemoveDuplicateOnce())
                {
                    changes.Add(new ExpressionChange("Свойство идемпотентности", _expr.ToString()));
                    transformed = true;
                }
                if (_expr.ApplyConstantsSimplificationOnce())
                {
                    changes.Add(new ExpressionChange("Упрощение констант", _expr.ToString()));
                    transformed = true;
                }
            }

            if (_expr.SortConjuncts())
            {
                changes.Add(new ExpressionChange("Сортировка конъюнктов", _expr.ToString()));
            }

            return changes;
        }

        private List<ExpressionChange> ToPDNF()
        {
            var changes = new List<ExpressionChange>();

            _expr.MakePDNFFromDNF();
            changes.Add(new ExpressionChange("Строим СДНФ", _expr.ToString()));

            bool transformed = true;
            while (transformed)
            {
                transformed = false;
                if (_expr.ApplyConstantsSimplificationOnce())
                {
                    changes.Add(new ExpressionChange("Упрощение констант после PDNF", _expr.ToString()));
                    transformed = true;
                }
                if (_expr.ApplyRemoveDuplicateOnce())
                {
                    changes.Add(new ExpressionChange("Удаляем дубликаты", _expr.ToString()));
                    transformed = true;
                }
            }

            if (_expr.SortConjuncts())
            {
                changes.Add(new ExpressionChange("Сортировка конъюнктов", _expr.ToString()));
            }

            return changes;
        }

        private List<ExpressionChange> ToANF()
        {
            var changes = new List<ExpressionChange>();

            _expr.ChangeNegativesToXorsInPDNF();
            changes.Add(new ExpressionChange("Заменили негативные литералы на \"1 xor литерал\"", _expr.ToString()));

            while (_expr.ExpandXorBrackets())
            {
                _expr.SortConjuncts();
                _expr.SimplifyConjuncts();
                changes.Add(new ExpressionChange("Раскрыли скобки", _expr.ToString()));
            }

            if (_expr.SortConjuncts())
            {
                changes.Add(new ExpressionChange("Сортируем конъюнкты", _expr.ToString()));
            }

            while (_expr.RemovePairInXor())
            {
                changes.Add(new ExpressionChange("Удаляем пару одинаковых", _expr.ToString()));
            }

            return changes;
        }
    }
}
