using System;
using System.Collections.Generic;
using System.Threading;

namespace Lucid.GoQuest
{
	//Production
	internal enum TeamStatus
	{
		None,
		Created,
		Waiting,
		Playing,
		GameOver,
		Archived,
		Unknown,
	}
	internal partial class Team : Base
	{
		private TeamStatus status = TeamStatus.Waiting;
		public TeamStatus PlayStatus { get { return status; } set { status = value; } }
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
			if ((g = GoQuest.Instance.gameversions.ClaimFirstEmpty(gamesPlayed)) != null)
			{
				//StartGame(g.ID);
				gamesPlayed.Add(g);
				g.Play(this, id, play);
			}
			else if (gamesPlayed.Count == GoQuest.Instance.gameversions.Count)
				Console.WriteLine("****************************Team {0} FINISHED, teams in play={1}", name, --TeamsInPlay);
			else
			{
				Thread.Sleep(100);
				play(o);
			}
		}
	}
}
