using System;
using System.IO;
using MilpManager.Abstraction;
using MilpManager.Implementation;

namespace LpSolveMilpManager.Implementation
{
    public class LpSolveMilpSolver  : BaseMilpSolver
    {
        public readonly IntPtr LpSolvePointer;
        public int Rows;
        public int Columns;

        private double[] GetEmptyArray(int count)
        {
            var array = new double[count];
            for (int i = 0; i < count; ++i)
            {
                array[i] = 0;
            }
            return array;
        }

        private double[] GetColumnArray()
        {
            return GetEmptyArray(Rows + 1);
        }

        private double[] GetRowArray()
        {
            return GetEmptyArray(Columns + 1);
        }

        private LpSolveVariable AddNewVariable()
        {
            Columns ++;
            var variable = new LpSolveVariable
            {
                Id = Columns,
                MilpManager = this
            };

            if (!lpsolve.add_column(LpSolvePointer, GetColumnArray()))
            {
                throw new InvalidOperationException();
            }

            return variable;
        }

        private int VariableId(IVariable v)
        {
            return (v as LpSolveVariable).Id;
        }
        private void AddRowConstraint(double[] row, lpsolve.lpsolve_constr_types type = lpsolve.lpsolve_constr_types.EQ, double val = 0)
        {
            Rows++;
            if (!lpsolve.add_constraint(LpSolvePointer, row, type, val))
            {
                throw new InvalidOperationException();
            }
        }

        public LpSolveMilpSolver(int integerWidth) : base(integerWidth)
        {
            LpSolvePointer = lpsolve.make_lp(0, 0);
        }

        protected override IVariable InternalSumVariables(IVariable first, IVariable second, Domain domain)
        {
            var newVariable = CreateAnonymous(domain);
            var row = GetRowArray();
            row[VariableId(first)] = 1;
            row[VariableId(second)] = 1;
            row[VariableId(newVariable)] = -1;
            AddRowConstraint(row);
            return newVariable;
        }

        protected override IVariable InternalNegateVariable(IVariable variable, Domain domain)
        {
            var newVariable = CreateAnonymous(domain);
            var row = GetRowArray();
            row[VariableId(variable)] = -1;
            row[VariableId(newVariable)] = -1;
            AddRowConstraint(row);
            return newVariable;
        }

        protected override IVariable InternalMultiplyVariableByConstant(IVariable variable, IVariable constant, Domain domain)
        {
            var newVariable = CreateAnonymous(domain) as LpSolveVariable;
            var row = GetRowArray();
            row[VariableId(variable)] = constant.ConstantValue.Value;
            row[VariableId(newVariable)] = -1;
            AddRowConstraint(row);
            return newVariable;
        }

        protected override IVariable InternalDivideVariableByConstant(IVariable variable, IVariable constant, Domain domain)
        {

            var newVariable = CreateAnonymous(domain);
            var row = GetRowArray();
            row[VariableId(variable)] = 1 / constant.ConstantValue.Value;
            row[VariableId(newVariable)] = -1;
            AddRowConstraint(row);
            return newVariable;
        }

        public override void SetLessOrEqual(IVariable variable, IVariable bound)
        {
            var row = GetRowArray();
            row[VariableId(variable)] = 1;
            row[VariableId(bound)] = -1;
            AddRowConstraint(row, lpsolve.lpsolve_constr_types.LE);
        }

        public override void SetEqual(IVariable variable, IVariable bound)
        {
            var row = GetRowArray();
            row[VariableId(variable)] = 1;
            row[VariableId(bound)] = -1;
            AddRowConstraint(row);
        }

        protected override IVariable InternalFromConstant(string name, int value, Domain domain)
        {
            return FromConstant((double)value, domain);
        }

        protected override IVariable InternalFromConstant(string name, double value, Domain domain)
        {
            var variable = Create(name, domain) as LpSolveVariable;
            var row = GetRowArray();
            row[VariableId(variable)] = 1;
            AddRowConstraint(row, lpsolve.lpsolve_constr_types.EQ, value);
            return variable;
        }

        protected override IVariable InternalCreate(string name, Domain domain)
        {
            var variable = AddNewVariable();
            variable.Domain = domain;
            variable.Name = name;
            if (!lpsolve.set_col_name(LpSolvePointer, variable.Id, name))
            {
                throw new InvalidOperationException();
            }
            if (variable.IsBinary())
            {
                if (!lpsolve.set_binary(LpSolvePointer, variable.Id, true))
                {
                    throw new InvalidOperationException();
                }
                return variable;
            }
            if (variable.IsInteger())
            {
                if (!lpsolve.set_int(LpSolvePointer, variable.Id, true))
                {
                    throw new InvalidOperationException();
                }
            }

            switch (domain)
            {
                case Domain.AnyInteger:
                case Domain.AnyReal:
                case Domain.AnyConstantInteger:
                case Domain.AnyConstantReal:
                    if (!lpsolve.set_unbounded(LpSolvePointer, variable.Id))
                    {
                        throw new InvalidOperationException();
                    }
                    break;
                case Domain.PositiveOrZeroInteger:
                case Domain.PositiveOrZeroReal:
                case Domain.PositiveOrZeroConstantInteger:
                case Domain.PositiveOrZeroConstantReal:
                    if (!lpsolve.set_lowbo(LpSolvePointer, variable.Id, 0))
                    {
                        throw new InvalidOperationException();
                    }
                    break;
                case Domain.BinaryInteger:
                case Domain.BinaryConstantInteger:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(domain), domain, null);
            }

            return variable;
        }

        public override void AddGoal(string name, IVariable operation)
        {
            var goal = GetRowArray();
            goal[VariableId(operation)] = 1;
            if (!lpsolve.set_obj_fn(LpSolvePointer, goal))
            {
                throw new InvalidOperationException();
            }
        }

        public override string GetGoalExpression(string name)
        {
            throw new NotImplementedException();
        }

        public override void SaveModelToFile(string modelPath)
        {
            if (Path.GetExtension(modelPath).Trim('.').ToLower() == "lp")
            {
                lpsolve.write_lp(LpSolvePointer, modelPath);
            }
            else
            {
                lpsolve.write_freemps(LpSolvePointer, modelPath);
            }
        }

        public override void LoadModelFromFile(string modelPath, string solverDataPath)
        {
            throw new NotImplementedException();
        }

        public override void SaveSolverDataToFile(string solverOutput)
        {
            throw new NotImplementedException();
        }

        public override void Solve()
        {
            lpsolve.solve(LpSolvePointer);
        }

        public override double GetValue(IVariable variable)
        {
            var row = GetRowArray();
            lpsolve.get_variables(LpSolvePointer, row);
            return row[VariableId(variable) - 1];
        }

        public override SolutionStatus GetStatus()
        {
            var status = lpsolve.get_status(LpSolvePointer);
            if (status == (int) lpsolve.lpsolve_return.OPTIMAL)
            {
                return SolutionStatus.Optimal;
            }
            if (status == (int)lpsolve.lpsolve_return.UNBOUNDED)
            {
                return SolutionStatus.Unbounded;
            }
            if (status == (int)lpsolve.lpsolve_return.INFEASIBLE)
            {
                return SolutionStatus.Infeasible;
            }

            return SolutionStatus.Unknown;
        }
    }
}