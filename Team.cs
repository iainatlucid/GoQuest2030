using Newtonsoft.Json;
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
		private ushort pinCode;
		private TeamStatus status = TeamStatus.Waiting;
		private List<GameVersion> gamesPlayed = new List<GameVersion>();
		private DateTime startTime = DateTime.MaxValue, endTime = DateTime.MaxValue;
		private List<string> gamesWon, gamesTried;
		private ushort score;
		internal int Played { get { return gamesPlayed.Count; } }
		internal Team(string n, int i) : this() { Name = n; ID = i; }
		internal Team(Team t) { }
		internal TeamStatus PlayStatus { get { return status; } set { status = value; } }
		internal ushort PinCode { get { return pinCode; } set { pinCode = value; } }
		internal DateTime StartTime { get { return startTime; } set { startTime = value; } }
		internal DateTime EndTime { get { return endTime; } set { endTime = value; } }
		internal List<string> GamesWon { get { return gamesWon; } set { gamesWon = value; } }
		internal List<string> GamesTried { get { return gamesTried; } set { gamesTried = value; } }
		internal ushort Score { get { return score; } set { score = value; } }
		[JsonIgnore] internal GameVersion Game;
		[JsonIgnore] public byte JustFailed { get; set; }

		public string RemainingShortTimeString(bool secs)
		{
			TimeSpan remain;
			switch (status)
			{
				case TeamStatus.Waiting:
					remain = new TimeSpan((endTime - startTime).Ticks);
					return string.Format("{0:0}:{1:00}{2}", remain.Hours, remain.Minutes, secs ? string.Format(":{0:00}", remain.Seconds) : "");
				case TeamStatus.Playing:
					remain = new TimeSpan((endTime - DateTime.Now).Ticks);
					return string.Format("{0:0}:{1:00}{2}", remain.Hours, remain.Minutes, secs ? string.Format(":{0:00}", remain.Seconds) : "");
				case TeamStatus.GameOver:
					return secs ? "0:00:00" : "0:00";
				default:
					return "--:--";
			}
		}
		public void Reset()
		{
			score = 0;
			Game = null;

			StdOut.WriteLine("{0} Reset", Name);

			startTime = DateTime.MaxValue;
			endTime = DateTime.MaxValue;
			gamesWon = new List<string>();
			gamesTried = new List<string>();
		}
		public TeamStatus TestStatus()
		{
			if (startTime.Year == 9999 && endTime.Year == 9999)
				return TeamStatus.Created;
			else if (startTime > DateTime.Now && endTime > DateTime.Now)
				return TeamStatus.Waiting;
			else if (startTime <= DateTime.Now && endTime >= DateTime.Now)
				return TeamStatus.Playing;
			else if (startTime < DateTime.Now && endTime < DateTime.Now)
				return TeamStatus.GameOver;
			else if (startTime.Year == 0 && endTime.Year == 0)
				return TeamStatus.Archived;
			else
			{
				StdOut.WriteLine("startTime {0} endTime{1}", startTime.ToString(), endTime.ToString());
				return TeamStatus.Unknown;
			}
		}
	}
	//Test
	internal partial class Team : Base
	{
		private bool quit;
		public static int TeamsInPlay = 0;
		internal Team() { TeamsInPlay++; Console.WriteLine("Teams in play: {0}", TeamsInPlay); }
		public Thread autoplay;
		internal void Autoplay() { autoplay = new Thread(play); autoplay.Start(); }
		internal Action<string> StartGame { get; set; }
		public void Stop()
		{
			StdOut.WriteLine("Team {0} stopping...", Name);
			quit = true;
		}
		private void play(object o)
		{
			if (quit) { StdOut.WriteLine("Team {0} stopped.", Name); return; }
			GameVersion g;
			if ((g = GoQuest.Instance.gameversions.ClaimFirstEmpty(gamesPlayed)) != null)
			{
				//StartGame(g.ID);
				gamesPlayed.Add(g);
				g.Play(this, ID, play);
			}
			else if (gamesPlayed.Count == GoQuest.Instance.gameversions.Count)
				Console.WriteLine("****************************Team {0} FINISHED, teams in play={1}", Name, --TeamsInPlay);
			else
			{
				Thread.Sleep(100);
				play(o);
			}
		}
	}
}
