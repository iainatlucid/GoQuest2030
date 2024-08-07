using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Lucid.GoQuest
{
	//Production
	internal partial class Team : Base
	{
		private int id;
		internal int Played { get { return gamesPlayed.Count; } }
		internal Team(string n, int i) :this() { name = n; id = i; }
		private List<GameVersion> gamesPlayed = new List<GameVersion>();
	}
	//Test
	internal partial class Team : Base
	{
		public static int TeamsInPlay = 0;
		internal Team() { TeamsInPlay++; Console.WriteLine("Teams in play: {0}", TeamsInPlay); }
		private Thread autoplay;
		internal void Autoplay() { autoplay = new Thread(play); autoplay.Start(); }
		internal Action<string> StartGame { get; set; }
		private void play(object o)
		{
			GameVersion g;
			if ((g = GoQuest2030.Instance.games.ClaimFirstEmpty(gamesPlayed)) != null)
			{
				//StartGame(g.ID);
				gamesPlayed.Add(g);
				g.Play(this, id, play);
			}
			else if (gamesPlayed.Count == GoQuest2030.Instance.games.Count)
				Console.WriteLine("****************************Team {0} FINISHED, teams in play={1}", name, --TeamsInPlay);
			else
			{
				Thread.Sleep(100);
				play(o);
			}
		}
	}
}
