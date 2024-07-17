using System;
using System.Threading;

namespace Lucid.GoQuest
{
	internal enum State { EMPTY, PLAYING }
	internal class Game : Base
	{
		public override string ToString() { return String.Format("{0}:{1}", name, State); }
		private TimerCallback gameOver { get; set; }
		internal volatile State State = State.EMPTY;
		private string currentTeam;
		internal void Play(Team team, int handicap, TimerCallback callback)
		{
			State = State.PLAYING;
			gameOver = callback;
			Console.WriteLine(">>>>>>>>>>{0} started playing {1}...", team, name);
			new Timer((o) =>
			//new Thread((o) =>
			{
				//Thread.Sleep(1000);
				Console.WriteLine("-----------------------{0} finished {1}, played {2}.", team, name, team.Played);
				State = State.EMPTY; 
				gameOver(o);
			}
			, null, 100, Timeout.Infinite);
			GoQuest2030.Instance.games.print();
		}
	}
}
