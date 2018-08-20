using System;
using System.Collections.Generic;
using MilpManager.Abstraction;

namespace LpSolveMilpManager.Implementation
{
	[Serializable]
	public class LpSolveVariable : ILpSolveVariable
	{
		[NonSerialized]
		private IMilpManager _milpManager;

		public IMilpManager MilpManager
		{
			get => _milpManager;
		    set => _milpManager = value;
		}

		public Domain Domain { get; set; }
		public double? ConstantValue { get; set; }
		public int Id { get; set; }
		public string Name { get; set; }
		public string Expression { get; set; }
	    public ICollection<string> Constraints { get; } = new List<string>();

        public override string ToString()
		{
			return $"[Name = {Name}, Domain = {Domain}, ConstantValue = {ConstantValue}, Id = {Id}, Expression = {Expression}";
		}
	}
}