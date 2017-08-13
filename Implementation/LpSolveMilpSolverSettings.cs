using System;
using System.IO;
using System.Reflection;
using LpSolveDotNet;
using MilpManager.Abstraction;

namespace LpSolveMilpManager.Implementation
{
	public class LpSolveMilpSolverSettings : MilpManagerSettings
	{
		public LpSolveMilpSolverSettings(bool initializeLp = true)
		{
			if (initializeLp)
			{
				var dllFolderPath = Path.Combine(
					Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath),
					Environment.Is64BitProcess ? @"NativeBinaries\win64\" : @"NativeBinaries\win32\");
				LpSolve.Init(dllFolderPath);
				LpSolvePointer = LpSolve.make_lp(0, 0);
			}
		}

		public LpSolve LpSolvePointer { get; set; }
	}
}