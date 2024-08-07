using System;
using System.Threading;

namespace Lucid.GoQuest
{
	internal enum State { EMPTY, PLAYING }
	//Production
	internal partial class GameVersion : Base 
	{
		internal void AddDelegates(GamesInterface gif)
		{
			gif.GameStarts.Add(name, gameStart);
			gif.SuperQuests.Add(name, superQuest);
			gif.GameNames.Add(name, gameName);
		}
		private void gameStart()
		{
			StdOut.WriteLine("GAME STARTED for {0}", name);
		}
		private void superQuest()
		{
			StdOut.WriteLine("SUPERQUEST called for {0}", name);
		}
		private void gameName()
		{
		}
	}
	//Test
	internal partial class GameVersion : Base
	{
		public override string ToString() { return String.Format("{0}:{1}", name, State); }
		private TimerCallback gameOver { get; set; }
		internal volatile State State = State.EMPTY;
		private string currentTeam;
		internal void Claim() { State = State.PLAYING; }
		internal void Play(Team team, int handicap, TimerCallback callback)
		{
			gameOver = callback;
			//Console.WriteLine(">>>>>>>>>>{0} started playing {1}...", team, name);
			Thread.Sleep(1000);	//playing...
			//Console.WriteLine("---------------{0} finished {1}, played {2}.", team, name, team.Played);
			State = State.EMPTY;
			gameOver(null);
		}
	}
}
