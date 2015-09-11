using System;
using System.IO;
using MilpManager.Abstraction;
using MilpManager.Implementation;

namespace LpSolveMilpManager.Implementation
{
    public class LpSolveMilpSolver  : BaseMilpSolver
    {
        public readonly IntPtr _lp;
        public int rows = 0;
        public int columns = 0;

        private double[] GetEmptyArray(int count)
        {
            return new double[count];
        }

        private double[] GetColumnArray()
        {
            return GetEmptyArray(rows + 1);
        }

        private double[] GetRowArray()
        {
            return GetEmptyArray(columns + 1);
        }

        private int tempId;

        private LpSolveVariable AddNewVariable()
        {
            columns ++;
            var variable = new LpSolveVariable
            {
                Id = columns,
                MilpManager = this
            };

            if (!lpsolve.add_column(_lp, GetColumnArray()))
            {
                throw new InvalidOperationException();
            }

            return variable;
        }

        public LpSolveMilpSolver(int integerWidth) : base(integerWidth)
        {
            _lp = lpsolve.make_lp(0, 0);
        }

        public override IVariable SumVariables(IVariable first, IVariable second, Domain domain)
        {
            var newVariable = CreateAnonymous(domain) as LpSolveVariable;
            var firstVariable = (first as LpSolveVariable);
            var secondVariable = (second as LpSolveVariable);
            newVariable.ConstantValue = firstVariable.ConstantValue + secondVariable.ConstantValue;
            var row = GetRowArray();
            row[firstVariable.Id] = 1;
            row[secondVariable.Id] = 1;
            row[newVariable.Id] = -1;
            AddRowConstraint(row);
            return newVariable;
        }

        public override IVariable NegateVariable(IVariable variable, Domain domain)
        {
            var newVariable = CreateAnonymous(domain) as LpSolveVariable;
            var castedVariable = (variable as LpSolveVariable);
            var variableValue = castedVariable.ConstantValue;
            newVariable.ConstantValue = -variableValue;
            var row = GetRowArray();
            row[castedVariable.Id] = -1;
            row[newVariable.Id] = -1;
            AddRowConstraint(row);
            return newVariable;
        }

        public override IVariable MultiplyVariableByConstant(IVariable variable, IVariable constant, Domain domain)
        {
            var newVariable = CreateAnonymous(domain) as LpSolveVariable;
            var castedVariable = (variable as LpSolveVariable);
            var constantValue = (constant as LpSolveVariable).ConstantValue;
            newVariable.ConstantValue = castedVariable.ConstantValue*constantValue;
            var row = GetRowArray();
            row[castedVariable.Id] = constantValue;
            row[newVariable.Id] = -1;
            AddRowConstraint(row);
            return newVariable;
        }

        private void AddRowConstraint(double[] row)
        {
            if (!lpsolve.add_constraint(_lp, row, lpsolve.lpsolve_constr_types.EQ, 0))
            {
                throw new InvalidOperationException();
            }
        }

        public override IVariable DivideVariableByConstant(IVariable variable, IVariable constant, Domain domain)
        {

            var newVariable = CreateAnonymous(domain) as LpSolveVariable;
            var castedVariable = (variable as LpSolveVariable);
            var constantValue = (constant as LpSolveVariable).ConstantValue;
            newVariable.ConstantValue = castedVariable.ConstantValue / constantValue;
            newVariable.Domain = domain;
            var row = GetRowArray();
            row[castedVariable.Id] = 1 / constantValue;
            row[newVariable.Id] = -1;
            AddRowConstraint(row);
            return newVariable;
        }

        public override void SetLessOrEqual(IVariable variable, IVariable bound)
        {
            var row = GetRowArray();
            row[(variable as LpSolveVariable).Id] = 1;
            row[(bound as LpSolveVariable).Id] = -1;
            if (!lpsolve.add_constraint(_lp, row, lpsolve.lpsolve_constr_types.LE, 0))
            {
                throw new InvalidOperationException();
            }
        }

        public override IVariable FromConstant(int value, Domain domain)
        {
            return FromConstant(1.0*value, domain);
        }

        public override IVariable FromConstant(double value, Domain domain)
        {
            var variable = CreateAnonymous(domain) as LpSolveVariable;
            variable.ConstantValue = value;
            var row = GetRowArray();
            row[variable.Id] = 1;
            if (!lpsolve.add_constraint(_lp, row, lpsolve.lpsolve_constr_types.EQ, value))
            {
                throw new InvalidOperationException();
            }
            return variable;
        }

        public override IVariable Create(string name, Domain domain)
        {
            var variable = AddNewVariable();
            variable.Domain = domain;
            variable.Name = name;
            if (!lpsolve.set_col_name(_lp, variable.Id, name))
            {
                throw new InvalidOperationException();
            }
            if (variable.IsBinary())
            {
                if (!lpsolve.set_binary(_lp, variable.Id, true))
                {
                    throw new InvalidOperationException();
                }
                return variable;
            }
            if (variable.IsInteger())
            {
                if (!lpsolve.set_int(_lp, variable.Id, true))
                {
                    throw new InvalidOperationException();
                }
            }
            if (!lpsolve.set_bounds(_lp, variable.Id, -Int32.MaxValue, Int32.MaxValue))
            {
                throw new InvalidOperationException();
            }
            return variable;
        }

        public override IVariable CreateAnonymous(Domain domain)
        {
            return Create($"v_{tempId++}", domain);
        }

        public override void AddGoal(string name, IVariable operation)
        {
            var goal = GetRowArray();
            goal[(operation as LpSolveVariable).Id] = 1;
            if (!lpsolve.set_obj_fn(_lp, goal))
            {
                throw new InvalidOperationException();
            }
        }

        public override string GetGoalExpression(string name)
        {
            throw new System.NotImplementedException();
        }

        public override void SaveModelToFile(string modelPath)
        {
            if (Path.GetExtension(modelPath) == "lp")
            {
                lpsolve.write_lp(_lp, modelPath);
            }
            else
            {
                lpsolve.write_freemps(_lp, modelPath);
            }
        }

        public override void LoadModelFromFile(string modelPath, string solverDataPath)
        {
            throw new System.NotImplementedException();
        }

        public override void SaveSolverDataToFile(string solverOutput)
        {
            throw new System.NotImplementedException();
        }

        public override IVariable GetByName(string name)
        {
            throw new System.NotImplementedException();
        }

        public override IVariable TryGetByName(string name)
        {
            throw new System.NotImplementedException();
        }

        public override void Solve()
        {
            lpsolve.solve(_lp);
        }

        public override double GetValue(IVariable variable)
        {
            var row = GetRowArray();
            lpsolve.get_variables(_lp, row);
            return row[(variable as LpSolveVariable).Id - 1];
        }

        public override SolutionStatus GetStatus()
        {
            var status = lpsolve.get_status(_lp);
            if (status == (int)lpsolve.lpsolve_return.OPTIMAL)
            {
                return SolutionStatus.Optimal;
            }

            return SolutionStatus.Unknown;
        }
    }
}