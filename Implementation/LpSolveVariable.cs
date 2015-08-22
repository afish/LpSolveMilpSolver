using MilpManager.Abstraction;

namespace LpSolveMilpSolver.Implementation
{
    public class LpSolveVariable : IVariable
    {
        public IMilpManager MilpManager { get; set; }
        public Domain Domain { get; set; }
        public double ConstantValue { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
    }
}