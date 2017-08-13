using MilpManager.Abstraction;

namespace LpSolveMilpManager.Implementation
{
	public interface ILpSolveVariable : IVariable
	{
		int Id { get; set; }
	}
}