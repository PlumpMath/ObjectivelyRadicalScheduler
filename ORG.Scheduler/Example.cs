using System;
using System.Collections.Generic;
using ObjectivelyRadical.Scheduler;

namespace SchedulerTest
{
	class MainClass
	{
		static Scheduler scheduler;
		public static void Main (string[] args)
		{
			scheduler = new Scheduler ();

			scheduler.Execute (TickTock, new string[] { "clock" });

			float timeToFixOneClock = 3f;
			string clockmakerName = "Gilligan";
			scheduler.ExecuteWithArgs<string, float> (Clockmaker, clockmakerName, timeToFixOneClock);

			// In a game with an update loop, you'd want to update your scheduler there instead
			while (true)
			{
				scheduler.Update (.0000008f);
			}
		}

		private static IEnumerator<ScriptPauser> TickTock ()
		{
			int i = 0;
			while (i < 8)
			{
				i++;
				if(i % 2 == 0)
					Console.WriteLine("Tock");
				else
					Console.WriteLine("Tick");

				yield return ScriptPauser.WaitSeconds(1f);
			}

			// Break the clock
			scheduler.PauseCoroutinesByTag("clock");

			// Tell the scheduler we've finished counting
			scheduler.SendSignal("Clock finished");

			/* We need a yield here to stop it from advancing
			 * We could just as easily have it await a signal, but we may 
			 * as well show off pausing and resuming */
			yield return null;

			i = 0;
			while (i < 100)
			{
				i++;
				if(i % 2 == 0)
					Console.WriteLine("Tock");
				else
					Console.WriteLine("Tick");
				
				yield return ScriptPauser.WaitSeconds(1f);
			}
		}

		private static IEnumerator<ScriptPauser> Clockmaker(string name, float timeToFix) 
		{
			string fullName = "Clockmaker " + name;
			yield return ScriptPauser.WaitForSignal("Clock finished");

			yield return ScriptPauser.WaitSeconds(3f);
			Console.WriteLine(fullName + ": It seems the clock has stopped.  " +
				"I'll be fixin' that right up.  That's my job, you see.");

			yield return ScriptPauser.WaitSeconds(timeToFix);
			scheduler.ResumeCoroutinesByTag("clock");	// Fix the clock

			yield return ScriptPauser.WaitSeconds(2f);
			Console.WriteLine(fullName + ": There we go!");
		}
	}
}
