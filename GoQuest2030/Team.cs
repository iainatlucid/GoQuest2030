using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Lucid.GoQuest
{
	internal class Team : Base
	{
		internal static int TeamsInPlay = 0;
		private int id;
		internal int Played { get { return played.Count; } }
		internal Team() { TeamsInPlay++; Console.WriteLine("Teams in play: {0}", TeamsInPlay); }
		internal Team(string n, int i) :this() { name = n; id = i; }
		private List<Game> played = new List<Game>();
		private Thread autoplay;
		internal void Autoplay() { autoplay = new Thread(play); autoplay.Start(); }
		private void play(object o)
		{
			Game g;
			if ((g = GoQuest2030.Instance.games.ClaimFirstEmpty(played)) != null)
			{
				played.Add(g);
				g.Play(this, id, play);
			}
			else if (played.Count == GoQuest2030.Instance.games.Count)
				Console.WriteLine("***********************************************Team {0} FINISHED, teams in play={1}", name, --TeamsInPlay);
			else
			{
				Thread.Sleep(100);
				play(o);
			}
		}
	}
}
