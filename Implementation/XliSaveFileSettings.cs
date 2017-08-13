using MilpManager.Abstraction;

namespace LpSolveMilpManager.Implementation
{
	public class XliSaveFileSettings : SaveFileSettings
	{
		public string Options { get; set; }
		public bool Results { get; set; }
	}
}