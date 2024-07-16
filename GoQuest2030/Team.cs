using System;
using System.Threading;

namespace Lucid.GoQuest
{
	internal class Team : Base
	{
		private int id;
		public int Played { get { return played.Count; } }
		internal Team() { }
		internal Team(string n, int i) { name = n; id = i; }
		private SafeList<Game> played = new SafeList<Game>();
		private Thread autoplay;
		internal void Autoplay() { autoplay = new Thread(play); autoplay.Start(); }
		private void play(object o)
		{
			while (true)
			{
				var game = GoQuest2030.Instance.games.FirstOrDefault(x => x.State == State.EMPTY);
				if (game != null && !played.Contains(game))
				{
					played.Add(game);
					game.Play(this, id, play);
				}
				if (played.Count == GoQuest2030.Instance.games.Count)
				{
					Console.WriteLine("{0} finished!", name);
					break;
				}
				Thread.Sleep(100);
			}
		}
	}
}
