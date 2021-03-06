﻿using System;
using System.IO;
using System.Reflection;
using LpSolveDotNet;
using MilpManager.Abstraction;
using MilpManager.Implementation;

namespace LpSolveMilpManager.Implementation
{
    public class LpSolveMilpSolver  : BaseMilpSolver
    {
        public LpSolve LpSolvePointer { get; private set; }
        public int Rows { get; private set; }
        public int Columns { get; private set; }

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

            if (!LpSolvePointer.add_column(GetColumnArray()))
            {
                throw new InvalidOperationException();
            }

            return variable;
        }

        private int VariableId(IVariable v)
        {
            return ((LpSolveVariable) v).Id;
        }
        private void AddRowConstraint(double[] row, lpsolve_constr_types type = lpsolve_constr_types.EQ, double val = 0)
        {
            Rows++;
            if (!LpSolvePointer.add_constraint(row, type, val))
            {
                throw new InvalidOperationException();
            }
        }

        public LpSolveMilpSolver(int integerWidth, double epsilon = 0.000000001) : base(integerWidth, epsilon)
        {
            var dllFolderPath = Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath), Environment.Is64BitProcess ? @"NativeBinaries\win64\" : @"NativeBinaries\win32\");
            LpSolve.Init(dllFolderPath);
            LpSolvePointer = LpSolve.make_lp(0, 0);
        }

        public LpSolveMilpSolver(LpSolve lpsolveInstance, int integerWidth, double epsilon) : base(integerWidth, epsilon)
        {
            LpSolvePointer = lpsolveInstance;
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
            AddRowConstraint(row, lpsolve_constr_types.LE);
        }

        public override void SetGreaterOrEqual(IVariable variable, IVariable bound)
        {
            var row = GetRowArray();
            row[VariableId(variable)] = 1;
            row[VariableId(bound)] = -1;
            AddRowConstraint(row, lpsolve_constr_types.GE);
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
            AddRowConstraint(row, lpsolve_constr_types.EQ, value);
            return variable;
        }

        protected override IVariable InternalCreate(string name, Domain domain)
        {
            var variable = AddNewVariable();
            variable.Domain = domain;
            variable.Name = name;
            if (!LpSolvePointer.set_col_name(variable.Id, name))
            {
                throw new InvalidOperationException();
            }
            if (variable.IsBinary())
            {
                if (!LpSolvePointer.set_binary(variable.Id, true))
                {
                    throw new InvalidOperationException();
                }
                return variable;
            }
            if (variable.IsInteger())
            {
                if (!LpSolvePointer.set_int(variable.Id, true))
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
                    if (!LpSolvePointer.set_unbounded(variable.Id))
                    {
                        throw new InvalidOperationException();
                    }
                    break;
                case Domain.PositiveOrZeroInteger:
                case Domain.PositiveOrZeroReal:
                case Domain.PositiveOrZeroConstantInteger:
                case Domain.PositiveOrZeroConstantReal:
                    if (!LpSolvePointer.set_lowbo(variable.Id, 0))
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

        protected override void InternalAddGoal(string name, IVariable operation)
        {
            var goal = GetRowArray();
            goal[VariableId(operation)] = 1;
            LpSolvePointer.set_maxim();
            if (!LpSolvePointer.set_obj_fn(goal))
            {
                throw new InvalidOperationException();
            }
        }

        public override void SaveModelToFile(string modelPath)
        {
            if (Path.GetExtension(modelPath)?.Trim('.').ToLower() == "lp")
            {
                LpSolvePointer.write_lp(modelPath);
            }
            else
            {
                LpSolvePointer.write_freemps(modelPath);
            }
        }

        protected override object GetObjectsToSerialize()
        {
            return new object[] {Rows, Columns};
        }

        protected override void InternalDeserialize(object data)
        {
            var array = (object[]) data;
            Rows = (int) array[0];
            Columns = (int) array[1];
        }

        protected override void InternalLoadModelFromFile(string modelPath)
        {
            LpSolvePointer.delete_lp();
            LpSolve.Init();
            var IMPORTANT = 3;
            var MPS_FREE = 8;
            LpSolvePointer = Path.GetExtension(modelPath)?.Trim('.').ToLower() == "lp" ? LpSolve.read_LP(modelPath, IMPORTANT , null) : LpSolve.read_MPS(modelPath, IMPORTANT  | MPS_FREE);
        }

        public override void Solve()
        {
            LpSolvePointer.solve();
        }

        public override double GetValue(IVariable variable)
        {
            var row = GetRowArray();
            LpSolvePointer.get_variables(row);
            return row[VariableId(variable) - 1];
        }

        public override SolutionStatus GetStatus()
        {
            var status = LpSolvePointer.get_status();
            if (status == (int)lpsolve_return.OPTIMAL)
            {
                return SolutionStatus.Optimal;
            }
            if (status == (int)lpsolve_return.UNBOUNDED)
            {
                return SolutionStatus.Unbounded;
            }
            if (status == (int)lpsolve_return.INFEASIBLE)
            {
                return SolutionStatus.Infeasible;
            }

            return SolutionStatus.Unknown;
        }
    }
}