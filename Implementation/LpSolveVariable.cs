using System;
using MilpManager.Abstraction;

namespace LpSolveMilpManager.Implementation
{
    [Serializable]
    public class LpSolveVariable : IVariable
    {
        [NonSerialized]
        private IMilpManager _milpManager;

        public IMilpManager MilpManager
        {
            get { return _milpManager; }
            set { _milpManager = value; }
        }

        public Domain Domain { get; set; }
        public double? ConstantValue { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Expression { get; set; }
        public override string ToString()
        {
            return $"[Name = {Name}, Domain = {Domain}, ConstantValue = {ConstantValue}, Id = {Id}, Expressoin = {Expression}";
        }
    }
}