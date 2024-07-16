using System;
using System.Threading;

namespace Lucid.GoQuest
{
	internal enum State { EMPTY, PLAYING }
	internal class Game : Base
	{
		private TimerCallback gameOver { get; set; }
		internal State State = State.EMPTY;
		private string currentTeam;
		internal void Play(Team team, int handicap, TimerCallback callback)
		{
			lock (this)
			{
				if (State == State.PLAYING) return;
				State = State.PLAYING;
			}
			gameOver = callback;
			Console.WriteLine("{0} started playing {1}, played {2}...", team.ToString(), name,team.Played);
			new Timer((o) => { State = State.EMPTY; gameOver(o); }, null, 10000 + 10 * handicap, Timeout.Infinite);
		}
	}
}
