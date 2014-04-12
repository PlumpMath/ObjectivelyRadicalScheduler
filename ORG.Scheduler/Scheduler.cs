using System;
using System.Collections.Generic;

namespace ObjectivelyRadical.Scheduler
{
	public delegate IEnumerator<ScriptPauser> Script();
	public delegate IEnumerator<ScriptPauser> ScriptWithArgs<T>(T args);
	public delegate IEnumerator<ScriptPauser> ScriptWithArgs<T,U>(T arg1, U arg2);
	public delegate IEnumerator<ScriptPauser> ScriptWithArgs<T,U,V>(T arg1, U arg2, V arg3);
	public delegate IEnumerator<ScriptPauser> ScriptWithArgs<T,U,V,W>(T arg1, U arg2, V arg3, W arg4);

	public class Scheduler
	{
		private List<ScriptWrapper> Scripts;
		private Dictionary<ScriptWrapper, float> WaitingForTime;
		private Dictionary<ScriptWrapper, string> WaitingForSignal;
		private List<string> SentSignals;

		private double currentTime = 0;

		public Scheduler ()
		{
			Initialize();
		}

		public void Initialize()
		{
			// Intialize lists and dictionaries
			Scripts = new List<ScriptWrapper>();
			WaitingForTime = new Dictionary<ScriptWrapper, float>();
			WaitingForSignal = new Dictionary<ScriptWrapper, string>();
			SentSignals = new List<string>();
		}

		public void PauseScript (ScriptWrapper script, ScriptPauser suspender)
		{
			if (suspender.Type == PauseScriptType.Seconds)
			{
				script.SetState(ScriptState.WaitingForTime);
				WaitingForTime.Add (script, (float)(currentTime + suspender.SleepTime));
			}
			else if (suspender.Type == PauseScriptType.Signal)
			{
				WaitingForSignal.Add(script, suspender.AwakenSignal);
				script.SetState(ScriptState.WaitingForSignal);
			}
		}

		#region Execute()

		/// <summary>
		/// Execute a script with no required arguments.
		/// </summary>
		/// <param name='script'>
		/// The script to execute.
		/// </param>
		/// <param name='tags'>
		/// An optional array of tags to apply to the script.
		/// </param>
		public void Execute(Script script, params string[] tags)
		{
			Scripts.Add(new ScriptWrapper(script, tags));
		}

		/// <summary>
		/// Execute a script with arguments.
		/// </summary>
		/// <param name='script'>
		/// The script to execute.
		/// </param>
		/// <param name='tags'>
		/// An optional array of tags to apply to the script.
		/// </param>
		public void ExecuteWithArgs<T>(ScriptWithArgs<T> script, T arg, params string[] tags)
		{
			Scripts.Add(ScriptWrapper.CreateScriptWrapperWithArgs(script, arg, tags));
		}

		/// <summary>
		/// Execute a script with arguments.
		/// </summary>
		/// <param name='script'>
		/// The script to execute.
		/// </param>
		/// <param name='tags'>
		/// An optional array of tags to apply to the script.
		/// </param>
		public void ExecuteWithArgs<T, U>(ScriptWithArgs<T, U> script, T arg1, U arg2, params string[] tags)
		{
			Scripts.Add(ScriptWrapper.CreateScriptWrapperWithArgs(script, arg1, arg2, tags));
		}

		/// <summary>
		/// Execute a script with arguments.
		/// </summary>
		/// <param name='script'>
		/// The script to execute.
		/// </param>
		/// <param name='tags'>
		/// An optional array of tags to apply to the script.
		/// </param>
		public void ExecuteWithArgs<T, U, V>(ScriptWithArgs<T, U, V> script, T arg1, U arg2, V arg3, params string[] tags)
		{
			Scripts.Add(ScriptWrapper.CreateScriptWrapperWithArgs(script, arg1, arg2, arg3, tags));
		}

		/// <summary>
		/// Execute a script with arguments.
		/// </summary>
		/// <param name='script'>
		/// The script to execute.
		/// </param>
		/// <param name='tags'>
		/// An optional array of tags to apply to the script.
		/// </param>
		public void ExecuteWithArgs<T, U, V, W>(ScriptWithArgs<T, U, V, W> script, T arg1, U arg2, V arg3, W arg4, params string[] tags)
		{
			Scripts.Add(ScriptWrapper.CreateScriptWrapperWithArgs(script, arg1, arg2, arg3, arg4, tags));
		}
		#endregion

		public void Update (double deltaTime)
		{
			// First, update the current time
			currentTime += deltaTime;

			// Remove all completed scripts from the list
			Scripts.RemoveAll (x => x.State == ScriptState.Completed);

			List<ScriptWrapper> reawakeningScripts = new List<ScriptWrapper> ();

			// Add all scripts awakened by time to the reawakening list
			foreach (KeyValuePair<ScriptWrapper, float> s in WaitingForTime)
			{
				if (currentTime >= s.Value)
				{
					reawakeningScripts.Add (s.Key);
				}
			}

			// Update all -currently- running scripts
			foreach (ScriptWrapper s in Scripts.FindAll(x => x.State == ScriptState.Running))
			{
				s.Update ((float)deltaTime, this);
			}
			
			// Add all signaled scripts to the reawakening list
			foreach (KeyValuePair<ScriptWrapper, string> s in WaitingForSignal)
			{
				if (SentSignals.Contains (s.Value))
					reawakeningScripts.Add (s.Key);
			}

			// Awaken all scripts at once
			foreach (ScriptWrapper s in reawakeningScripts)
			{
				if(WaitingForSignal.ContainsKey(s))
					WaitingForSignal.Remove(s);

				if(WaitingForTime.ContainsKey(s))
					WaitingForTime.Remove(s);

				s.SetState (ScriptState.Running);

				// Be polite and let them update this cycle
				s.Update((float)deltaTime, this);

			}

			// Clear all signals
			SentSignals.Clear();
		}


		/// <summary>
		/// Sends a named signal to all coroutines.
		/// </summary>
		/// <param name='signal'>
		/// The signal to send
		/// </param>
		public void SendSignal(string signal)
		{
			SentSignals.Add(signal);
		}


		/// <summary>
		/// Aborts all coroutines.
		/// </summary>
		public void AbortAllCoroutines ()
		{
			// Avoid modifying the collection by setting each script to Completed
			foreach (ScriptWrapper s in Scripts)
			{
				s.SetState(ScriptState.Completed);
			}

			WaitingForSignal.Clear();
			WaitingForTime.Clear();
			SentSignals.Clear();
		}


		/// <summary>
		/// Pauses all coroutines registered with a given tag.
		/// </summary>
		/// <param name='tag'>
		/// The tag to pause
		/// </param>
		public void PauseCoroutinesByTag (string tag)
		{
			foreach (ScriptWrapper s in Scripts.FindAll(x => x.Tags.Contains(tag)))
			{
				s.Pause();
			}
		}


		/// <summary>
		/// Resumes all coroutines registered with a given tag.
		/// </summary>
		/// <param name='tag'>
		/// The tag to resume
		/// </param>
		public void ResumeCoroutinesByTag (string tag)
		{
			foreach (ScriptWrapper s in Scripts.FindAll(x => x.Tags.Contains(tag)))
			{
				// Each script will check that it is paused before modifying state
				s.Resume();
			}
		}


		/// <summary>
		/// Aborts all coroutines registered with a given tag.
		/// </summary>
		/// <param name='tag'>
		/// The tag to abort
		/// </param>
		public void AbortCoroutinesByTag (string tag)
		{
			// Avoid modifying the collection by setting each script to Completed
			foreach (ScriptWrapper s in Scripts.FindAll(x => x.Tags.Contains(tag)))
			{
				s.SetState (ScriptState.Completed);
			}

			// Create a list to store references to canceled coroutines
			List<ScriptWrapper> aborted = new List<ScriptWrapper> ();
			foreach (KeyValuePair<ScriptWrapper, string> s in WaitingForSignal)
			{
				if (s.Key.Tags.Contains (tag))
					aborted.Add (s.Key);
			}

			foreach (KeyValuePair<ScriptWrapper, float> s in WaitingForTime)
			{
				if (s.Key.Tags.Contains (tag))
					aborted.Add (s.Key);
			}

			foreach (ScriptWrapper s in aborted)
			{
				WaitingForSignal.Remove(s);
				WaitingForTime.Remove(s);
			}
		}
	}
}

