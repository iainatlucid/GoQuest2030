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
	internal partial class Team
	{
		//SQL VARS
		private int id;
		private string name;
		private ushort score;
		private DateTime startTime = DateTime.MaxValue, endTime = DateTime.MaxValue;
		//END SQL
		private ushort pinCode;
		private SafeList<GameVersion> gamesWon, gamesTried;
		private TeamStatus status = TeamStatus.Unknown;
		private byte avgAge, totalMembers;

		private static Random pinGenerator;

		//SQL VARS
		public int ID { get { return id; } set { id = value; } }
		public string Name { get { return name; } set { name = value; } }
		public ushort Score { get { return score; } set { score = value; } }
		public DateTime StartTime { get { return startTime; } set { startTime = value; } }
		public DateTime EndTime { get { return endTime; } set { endTime = value; } }
		//END SQL
		public byte AvgAge { get { return avgAge; } set { avgAge = value; } }
		public byte TotalMembers { get { return totalMembers; } set { totalMembers = value; } }
		public ushort PinCode { get { return pinCode; } set { pinCode = value; } }
		public SafeList<GameVersion> GamesWon { get { return gamesWon; } set { gamesWon = value; } }
		public SafeList<GameVersion> GamesTried { get { return gamesTried; } set { gamesTried = value; } }
		public TeamStatus PlayStatus { get { return status; } set { status = value; } }
		[JsonIgnore]
		public GameVersion Game;
		[JsonIgnore]
		public byte JustFailed { get; set; }

		public Team()
		{
			gamesWon = new SafeList<GameVersion>();
			gamesTried = new SafeList<GameVersion>();
			init_test();
		}
		public Team(string name)
		{
			Reset();
			PinCode = 0;
			Name = name;
			PlayStatus = TestStatus();
		}
		public Team(Team t)
		{
			name = t.name;
			avgAge = t.avgAge;
			totalMembers = t.totalMembers;
			score = t.score;
			startTime = t.startTime;
			endTime = t.endTime;
			pinCode = t.pinCode;
			gamesWon = t.gamesWon;
			gamesTried = t.gamesTried;
			status = t.status;
			Game = t.Game;
		}
		public void Modify(Team t)
		{
			name = t.name;
			avgAge = t.avgAge;
			totalMembers = t.totalMembers;
			score = t.score;
			startTime = t.startTime;
			endTime = t.endTime;
			pinCode = t.pinCode;
			gamesWon = t.gamesWon;
			gamesTried = t.gamesTried;
			status = t.status;
			Game = t.Game;
		}
		public ushort AssignRandomPin()
		{
			if (pinGenerator == null)
				pinGenerator = new Random((int)DateTime.Now.Ticks);
			return (this.pinCode = (ushort)pinGenerator.Next
				((int)(Math.Pow(10, GoQuest.Instance.PinLength - 1) + 0.5),
					(int)(Math.Pow(10, GoQuest.Instance.PinLength) - 1 + 0.5)));
		}
		public void StartNow(short secs)
		{
			startTime = DateTime.Now;
			endTime = new DateTime(startTime.Ticks + TimeSpan.TicksPerSecond * secs);
		}
		public void Extend(short secs)
		{
			endTime = new DateTime(endTime.Ticks + secs * TimeSpan.TicksPerSecond);
		}
		public void Shift(short secs)
		{
			endTime = new DateTime(endTime.Ticks + secs * TimeSpan.TicksPerSecond);
			startTime = new DateTime(startTime.Ticks + secs * TimeSpan.TicksPerSecond);
		}
		public long RemainingSecs()
		{
			return (endTime - DateTime.Now).Ticks / TimeSpan.TicksPerSecond;
		}
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
			/*
			TimeSpan remain;
			if (startTime > DateTime.Now)
			{
				if (endTime > DateTime.Now)
				{
					remain=new TimeSpan((endTime-startTime).Ticks);
					return startTime == endTime ? "--:--" : string.Format("{0:0}:{1:00}:{2:00}", remain.Hours, remain.Minutes, remain.Seconds);
				}

			}
			 */
			/*
			var ticks = (endTime - DateTime.Now).Ticks;
			var remain = new TimeSpan(ticks);

			return
				ticks > 0 ?
					status == TeamStatus.Playing ?
						string.Format("{0:0}:{1:00}:{2:00}", remain.Hours, remain.Minutes, remain.Seconds)
						: "--:--"
				: "00:00";
		*/
		}
		public ushort RemainingPer16Bit()
		{
			//TODO: This wraps on the TeamConfirm gauge when it goes negative upon GAME_OVER
			return (ushort)(endTime == startTime ? 0 : DateTime.Now > endTime ? 0 : ((endTime - DateTime.Now).Ticks * 65535 / (endTime - startTime).Ticks));
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
		public void CheckPlay()
		{
			var v = TestStatus();
			if (v != status)
			{
				status = v;
				StdOut.WriteLine("{0} {1}", name, status.ToString());
				//GoQuest.Instance.Terminal.LastCmd = "list teams";
				//GoQuest.Instance.serialise();
			}
		}
		public void Reset()
		{
			score = 0;
			Game = null;

			//StdOut.WriteLine("{0} Reset", Name);

			startTime = DateTime.MaxValue;
			endTime = DateTime.MaxValue;
			gamesWon = new SafeList<GameVersion>();
			gamesTried = new SafeList<GameVersion>();
		}

		#region IComparable Members

		public static int Compare(Team a, Team b)
		{
			return b.Score - a.Score;
		}

		#endregion
	}
	//Test
	internal partial class Team
	{
		private bool quit;
		[JsonIgnore] public static int TeamsInPlay = 0;
		private void init_test() { TeamsInPlay++; StdOut.WriteLine("Teams in play: {0}", TeamsInPlay); }
		[JsonIgnore] public Thread autoplay;
		internal void Autoplay() { autoplay = new Thread(play); autoplay.Start(); }
		[JsonIgnore] internal Action<string> StartGame { get; set; }
		internal Team(string n, int i) : this() { Name = n; ID = i; }
		public void Stop()
		{
			StdOut.WriteLine("Team {0} stopping...", Name);
			quit = true;
		}
		private void play(object o)
		{
			if (quit) { StdOut.WriteLine("Team {0} stopped.", Name); return; }
			GameVersion g;
			if ((g = GoQuest.Instance.gameversions.ClaimFirstEmpty(gamesTried)) != null)
			{
				g.Play(this);
				gamesTried.Add(g);
			}
			else if (gamesTried.Count == GoQuest.Instance.gameversions.Count)
			{
				Console.WriteLine("********************** Team '{0}' FINISHED", Name);
				gamesTried.Clear();
			}
			else
				Thread.Sleep(100);
			play(o);
		}
	}
}
