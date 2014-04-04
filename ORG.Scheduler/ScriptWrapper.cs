using System;
using System.Collections.Generic;

namespace ObjectivelyRadical.Scheduler
{
	public class ScriptWrapper
	{
		#region Members and properties
		// Stores the script itself as an IEnumerator
		IEnumerator<ScriptPauser> thisScript;

		// The current state of this script
		public ScriptState State { get; private set; }
		private ScriptState _lastState;

		// Stores a list of tags that this script is using.
		public List<string> Tags { get; private set; }

		// Returns the current ScriptSuspender that the IEnumerator has stopped over
		private ScriptPauser Current
		{
			get { return thisScript.Current; }
		}

		#endregion
		public ScriptWrapper()
		{
		}

		public ScriptWrapper (Script script, params string[] tags)
		{
			thisScript = script ();
			State = ScriptState.Running;

			// Build the list of tags
			Tags = new List<string> ();
			foreach (string s in tags)
			{
				Tags.Add(s);
			}
		}

		public static ScriptWrapper CreateScriptWrapperWithArgs<T> (ScriptWithArgs<T> script, T arg, params string[] tags)
		{
			ScriptWrapper newWrapper = new ScriptWrapper();
			newWrapper.thisScript = script(arg);
			InitializeWrapper(newWrapper);

			return newWrapper;
		}

		public static ScriptWrapper CreateScriptWrapperWithArgs<T, U> (ScriptWithArgs<T, U> script, T arg1, U arg2, params string[] tags)
		{
			ScriptWrapper newWrapper = new ScriptWrapper();
			newWrapper.thisScript = script(arg1, arg2);
			InitializeWrapper(newWrapper);
			
			return newWrapper;
		}

		public static ScriptWrapper CreateScriptWrapperWithArgs<T, U, V> (ScriptWithArgs<T, U, V> script, T arg1, U arg2, V arg3, params string[] tags)
		{
			ScriptWrapper newWrapper = new ScriptWrapper();
			newWrapper.thisScript = script(arg1, arg2, arg3);
			InitializeWrapper(newWrapper);

			return newWrapper;
		}

		public static ScriptWrapper CreateScriptWrapperWithArgs<T, U, V, W> (ScriptWithArgs<T, U, V, W> script, T arg1, U arg2, V arg3, W arg4, params string[] tags)
		{
			ScriptWrapper newWrapper = new ScriptWrapper();
			newWrapper.thisScript = script(arg1, arg2, arg3, arg4);
			InitializeWrapper(newWrapper);
			
			return newWrapper;
		}


		private static void InitializeWrapper (ScriptWrapper newWrapper, params string[] tags)
		{
			newWrapper.State = ScriptState.Running;
			
			// Build the list of tags
			newWrapper.Tags = new List<string> ();
			foreach (string s in tags)
			{
				newWrapper.Tags.Add(s);
			}
		}


		// Moves the script to the next yield and returns true if it exists
		public bool MoveNext()
		{
			if (thisScript.MoveNext())
				return true;
			else
				return false;
		}


		public void SetState (ScriptState state)
		{
			State = state;
		}


		public void Update (float deltaTime, Scheduler thisScheduler)
		{
			if (State != ScriptState.Running)
				return;

			else if (State == ScriptState.Running)
			{
				if (MoveNext ())
				{
					if (Current != null)
					{
						thisScheduler.PauseScript (this, Current);
					}
				}
				else
				{
					State = ScriptState.Completed;
				}
			}
		}

		/// <summary>
		/// Pauses the script until manually resumed.
		/// </summary>
		public void Pause()
		{
			_lastState = State;
			SetState(ScriptState.Paused);
		}

		/// <summary>
		/// Returns the script to its previous state.
		/// </summary>
		public void Resume ()
		{
			if (State == ScriptState.Paused)
			{
				SetState(_lastState);
			}
		}
	}

	public enum ScriptState
	{
		Running,
		Paused,
		WaitingForTime,
		WaitingForSignal,
		Completed
	}
}

