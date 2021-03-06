using System;
using System.Collections.Generic;

namespace ObjectivelyRadical.Scheduler
{
	public delegate IEnumerator<ScriptPauser> Script();
	public delegate IEnumerator<ScriptPauser> ScriptWithArgs<T>(T args);
	public delegate IEnumerator<ScriptPauser> ScriptWithArgs<T,U>(T arg1, U arg2);
	public delegate IEnumerator<ScriptPauser> ScriptWithArgs<T,U,V>(T arg1, U arg2, V arg3);
	public delegate IEnumerator<ScriptPauser> ScriptWithArgs<T,U,V,W>(T arg1, U arg2, V arg3, W arg4);
	public delegate IEnumerator<ScriptPauser> ScriptWithArgs<T,U,V,W,X>(T arg1, U arg2, V arg3, W arg4, X arg5);
	public delegate IEnumerator<ScriptPauser> ScriptWithArgs<T,U,V,W,X,Y>(T arg1, U arg2, V arg3, W arg4, X arg5,
	                                                                      Y arg6);
	public delegate IEnumerator<ScriptPauser> ScriptWithArgs<T,U,V,W,X,Y,Z>(T arg1, U arg2, V arg3, W arg4, X arg5,
	                                                                      Y arg6, Z arg7);

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

		public void ExecuteWithArgs<T, U, V, W, X>(ScriptWithArgs<T, U, V, W, X> script, 
		                                           T arg1, U arg2, V arg3, W arg4, X arg5, params string[] tags)
		{
			Scripts.Add(ScriptWrapper.CreateScriptWrapperWithArgs(script, arg1, arg2, arg3, arg4, arg5, tags));
		}

		public void ExecuteWithArgs<T, U, V, W, X, Y>(ScriptWithArgs<T, U, V, W, X, Y> script, 
		                                           T arg1, U arg2, V arg3, W arg4, X arg5, Y arg6, params string[] tags)
		{
			Scripts.Add(ScriptWrapper.CreateScriptWrapperWithArgs(script, arg1, arg2, arg3, arg4, arg5, arg6, tags));
		}

		public void ExecuteWithArgs<T, U, V, W, X, Y, Z>(ScriptWithArgs<T, U, V, W, X, Y, Z> script, 
		                                              T arg1, U arg2, V arg3, W arg4, X arg5, Y arg6, Z arg7, params string[] tags)
		{
			Scripts.Add(ScriptWrapper.CreateScriptWrapperWithArgs(script, arg1, arg2, arg3, arg4, arg5, arg6, arg7, tags));
		}
		#endregion

		public void Update (double deltaTime)
		{
			// First, update the current time
			currentTime += deltaTime;

			// Clear sent signals
			SentSignals.Clear();

			// Remove all completed scripts from the list
			Scripts.RemoveAll (x => x.State == ScriptState.Completed);



			// Update all -currently- running scripts
			foreach (ScriptWrapper s in Scripts.FindAll(x => x.State == ScriptState.Running))
			{
				s.Update ((float)deltaTime, this);
			}

			ReawakenTimeScripts(deltaTime);
			ReawakenSignalScripts(deltaTime);
		}

		private void ReawakenTimeScripts (double deltaTime)
		{
			List<ScriptWrapper> reawakeningScripts = new List<ScriptWrapper> ();

			// Add all scripts awakened by time to the reawakening list
			foreach (KeyValuePair<ScriptWrapper, float> s in WaitingForTime)
			{
				if (currentTime >= s.Value)
				{
					reawakeningScripts.Add (s.Key);
				}
			}

			// Awaken the time scripts
			foreach (ScriptWrapper s in reawakeningScripts)
			{
				// Because it's possible for scripts to cancel other scripts, we need to make sure
				// that this script has not yet completed before running it
				if(s.State == ScriptState.Completed)
					continue;
				
				s.SetState(ScriptState.Running);

				// Verify that we haven't grabbed an invalid script
				if(WaitingForTime.ContainsKey(s))
				{
					WaitingForTime.Remove(s);
					
					// Be polite and let time sensitive scripts update this cycle
					s.Update((float)deltaTime, this);
				}
			}
		}

		private void ReawakenSignalScripts (double deltaTime)
		{
			List<ScriptWrapper> reawakeningScripts = new List<ScriptWrapper> ();

			// Add all signaled scripts to the reawakening list
			foreach (KeyValuePair<ScriptWrapper, string> s in WaitingForSignal)
			{
				if (SentSignals.Contains (s.Value))
					reawakeningScripts.Add (s.Key);
			}

			// Awaken the signal scripts
			foreach (ScriptWrapper s in reawakeningScripts)
			{
				// Because it's possible for scripts to cancel other scripts, we need to make sure
				// that this script has not yet completed before running it
				if(s.State == ScriptState.Completed)
					continue;
				
				s.SetState(ScriptState.Running);
				
				if(WaitingForSignal.ContainsKey(s))
				{
					WaitingForSignal.Remove(s);
					
					// Don't let signalled script resume now, just in case they send their own signals
				}
			}
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

		// Returns a list of all coroutines (for debugging purposes only)
		public List<ScriptWrapper> GetAllCoroutines ()
		{
			return Scripts;
		}

		// Returns a list of all coroutines with the given tag(for debugging purposes only)
		public List<ScriptWrapper> GetCoroutinesByTag (string tag)
		{
			return Scripts.FindAll(x => x.Tags.Contains(tag));
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

		// Aborts all coroutines that do not have the given tag assigned to them
		public void AbortCoroutinesWithoutTag(string tag)
		{
			// Avoid modifying the collection by setting each script to Completed
			foreach (ScriptWrapper s in Scripts.FindAll(x => !x.Tags.Contains(tag)))
			{
				s.SetState (ScriptState.Completed);
			}

			// Create a list to store references to canceled coroutines
			List<ScriptWrapper> aborted = new List<ScriptWrapper> ();
			foreach (KeyValuePair<ScriptWrapper, string> s in WaitingForSignal)
			{
				if (!s.Key.Tags.Contains (tag))
					aborted.Add (s.Key);
			}
			
			foreach (KeyValuePair<ScriptWrapper, float> s in WaitingForTime)
			{
				if (!s.Key.Tags.Contains (tag))
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

