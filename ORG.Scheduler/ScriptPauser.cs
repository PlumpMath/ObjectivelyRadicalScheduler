using System;

namespace ObjectivelyRadical.Scheduler
{
	public class ScriptPauser
	{
		public PauseScriptType Type { get; private set; }
		public float SleepTime { get; set; }
		public string AwakenSignal { get; set; }


		public ScriptPauser (double time)
		{
			Type = PauseScriptType.Seconds;
			SleepTime = (float)time;
		}

		public ScriptPauser(string signal)
		{
			Type = PauseScriptType.Signal;
			AwakenSignal = signal;
		}

		public static ScriptPauser WaitSeconds(double time)
		{
			return new ScriptPauser(time);
		}

		public static ScriptPauser WaitForSignal(string signal)
		{
			return new ScriptPauser(signal);
		}
	}

	public enum PauseScriptType
	{
		Seconds,	// Wait a number of seconds until resume
		Signal		// Wait until a signal is raised to resume
	}
}

