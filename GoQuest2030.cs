using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Lucid.GoQuest
{
	public static class StdOut
	{
		public static Action<string> WriteStr { get; set; }
		public static Action<string, object[]> WriteFmt { get; set; }
		public static void WriteLine(string str) { WriteStr(str); }
		public static void WriteLine(string str, params object[] args) { WriteFmt(str, args); }
	}
	internal class SafeList<T>
	{
		[JsonProperty] protected List<T> list;
		public T this[int key] { get { lock (this) return list[key]; } set { lock (this) list[key] = value; } }
		[JsonIgnore] public int Count { get { lock (this) return list.Count; } }
		internal SafeList() { list = new List<T>(); }
		internal void Add(T elem) { lock (this) list.Add(elem); }
		internal bool TryAdd(T elem) { lock (this) { if (list.Contains(elem)) return false; Add(elem); return true; } }
		internal void Clear() { lock (this) list.Clear(); }
		internal bool TryClear(Func<T, bool> pred)
		{
			var del = new List<T>();
			lock (this)
			{
				foreach (var l in list)
					if (pred(l))
						del.Add(l);
				foreach (var d in del)
					list.Remove(d);
				return list.Count == 0;
			}
		}
		internal bool Contains(T elem) { lock (this) return list.Contains(elem); }
		internal bool Remove(T elem) { lock (this) return list.Remove(elem); }
		internal void ForEach(Action<T> act) { lock (this) foreach (var t in list) act(t); }
		internal bool ForEach_ReturnOnSuccess(Func<T, bool> func) { lock (this) foreach (var l in list) if (func(l)) return true; return false; }
		internal bool Any(Func<T, bool> pred) { lock (this) { return Where(pred).Count() > 0; } }
		internal IEnumerable<T> Where(Func<T, bool> pred) { lock (this) return list.Where(pred); }
		internal T FirstOrDefault(Func<T, bool> pred) { lock (this) return list.Where(pred).FirstOrDefault(); }
		internal IEnumerable<T> Intersect(IEnumerable<T> set) { lock (this) lock (set) return list.Intersect(set); }
		internal void InsertionSort(Comparison<T> comparison)
		{
			lock (this)
			{
				int count = list.Count;
				for (int j = 1; j < count; j++)
				{
					T key = list[j];

					int i = j - 1;
					for (; i >= 0 && comparison(list[i], key) > 0; i--)
					{
						list[i + 1] = list[i];
					}
					list[i + 1] = key;
				}
			}
		}
		internal void print() { lock (this) foreach (var v in list) StdOut.WriteLine(v.ToString()); }
	}
	internal class SafeGameVersionsList : SafeList<GameVersion>
	{
		internal GameVersion ClaimFirstEmpty(SafeList<GameVersion> played)
		{
			lock (list)
			{
				var empties = Where(x => x.PlayState == GameState.EMPTY).ToList();
				var intersect = played.Intersect(empties);
				empties.RemoveAll(x => intersect.Contains(x));
				if (empties.Any()) { empties.First().Claim(); return empties.First(); }
				return null;
			}
		}
		internal void AddDelegates(GamesInterface gif)
		{
			lock (list)
				foreach (var g in list)
					g.AddDelegates(gif);
		}
	}
	public class GoQuest
	{
		private static readonly byte MAJOR_REV = 2, MINOR_REV = 0, RELEASE_REV = 1;
		private static readonly string REV_SUFFIX = "";
		private static GoQuest instance = null;
		private GamesInterface gif = new GamesInterface();
		private Thread gameloop;
		private bool quit;
		internal static bool cleaning;
		internal ushort ButtonHoldTime { get; set; } = 1000;    //todo
		internal ushort TempFlashPeriod { get; set; } = 500; //todo
		internal ushort DefaultTeamMins { get; set; } = 75; //todo
		internal ushort PinLength { get; set; } = 3; //todo
		[JsonProperty] private SafeList<ushort> teampins;
		[JsonProperty] internal SafeList<Team> teams;
		[JsonProperty] internal SafeGameVersionsList gameversions;
		[JsonProperty] private SafeList<UserControlPanel> userpanels;
		[JsonIgnore] public static bool Autoplay { get; set; }
		[JsonIgnore] public static string Path { get; set; }
		[JsonIgnore] public static GoQuest Instance { get { return instance == null ? instance = deserialise() : instance; } }
		[JsonIgnore] public static volatile ushort cpuload;
		private GoQuest() { }
		private GoQuest detokenise()
		{
			return this;
		}
		private static GoQuest deserialise()
		{
			string s;
			using (var file = new StreamReader(new FileStream(Path + "goquest.json", FileMode.Open)))
				s = file.ReadToEnd();
			return JsonConvert.DeserializeObject<GoQuest>(s, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto }).detokenise().init();
		}
		private void tokenise()
		{
		}
		internal void serialise()
		{
			tokenise();
			var s = JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
			if (File.Exists(Path + "goquest.json"))
			{
				try { File.Delete(Path + "goquest.json"); }
				catch (Exception e) { StdOut.WriteLine("EXCEPTION: serialise(): Cannot delete file: \r\n{0}", e.StackTrace); }
			}
			using (var file = File.CreateText(Path + "goquest.json"))
				file.Write(s);
			detokenise();
		}
		public static void Start(object arg)
		{
			Instance.userpanels.ForEach((p) => p.Initialise(arg, Instance));
			Instance.gameloop = new Thread(Instance.run);
			Instance.gameloop.Start();
			//Instance.sql = new SQL(SqlServerStr, SqlDatabase, SqlLogTo);
		}

		private void run()
		{
			uint c = 0;
			while (!quit)
			{
				try
				{
					Instance.teams.ForEach((t) => t.CheckPlay());
					if (c % 4 == 0) Instance.teams.InsertionSort(Team.Compare);
					Instance.userpanels.ForEach((p) => p.Update());
				}
				catch (Exception e)
				{
					StdOut.WriteLine(">>> EXCEPTION: GoQuest2030.run: \r\n{0}", e.StackTrace);
				}
				c = c < uint.MaxValue ? c + 1 : 0;
#if !DEBUG
				//watchdog.Reset(WatchdogDelay);
#endif
				Thread.Sleep(250);
			}
			StdOut.WriteLine("Game loop stopped.");
		}
		public static void Stop()
		{
			StdOut.WriteLine("GoQuest stopping...");
			Instance.gif.Stop();
			Instance.teams.ForEach((t) => t.Stop());
			StdOut.WriteLine("GoQuest stopped.");
		}
		public static void Print()
		{
			StdOut.WriteLine(JsonConvert.SerializeObject(Instance, Formatting.Indented));
		}
		private GoQuest init()
		{
			gameversions.AddDelegates(gif);
			return this;
		}
		internal static void AddTeam(Team team)
		{
			if (Autoplay) team.Autoplay();
			Instance.teams.Add(team);
			Instance.serialise();
			/*
			lock (objectsync)
			{
				Instance.teams.Add(team);
				sql.AddTeam(team);
				Instance.serialise("GoQuest.AddTeam", team.Name);
			}
			*/
		}
		public static bool AddTeam(string name)
		{
			var team = new Team(name);
			if (Autoplay) team.Autoplay();
			var ret = Instance.teams.TryAdd(team);
			Instance.serialise();
			return ret;
			/*
			lock (objectsync)
			{
				foreach (var t in Instance.teams)
					if (t.Name.Equals(name))
						return false;
				var team = new Team(name);
				Instance.teams.Add(team);
				sql.AddTeam(team);
				Instance.serialise("GoQuest.AddTeam", name);
				return true;
			}
			*/
		}
		internal static void ModifyTeam(Team orgTeam, Team newTeam)
		{
			Instance.teams.ForEach
			(
				(t) =>
				{
					if (t == orgTeam)
						t.Modify(newTeam);
				}
			);
			Instance.serialise();
			/*
						lock (objectsync)
						{
							foreach (var t in Instance.teams)
								if (t == orgTeam)
								{
									t.Modify(newTeam);
									sql.ModifyTeam(t);
									Instance.serialise("GoQuest.ModifyTeam", t.Name);
								}
						}
						*/
		}
		internal static bool DeleteTeam(Team team)
		{
			return Instance.teams.ForEach_ReturnOnSuccess
			(
				(t) =>
				{
					if (t == team && t.PlayStatus != TeamStatus.Playing && t.Game == null)
					{
						Instance.teams.Remove(t);
						return true;
					}
					return false;
				}
			);
			/*
			lock (objectsync)
			{
				foreach (var t in Instance.teams)
					if (t == team && t.PlayStatus != TeamStatus.Playing && t.Game == null)
					{
						Instance.teams.Remove(t);
						Instance.serialise("GoQuest.DeleteTeam", t.Name);
						return true;
					}
			}
			return false;
			*/
		}
		public static void ToggleCleaning()
		{
			if (Instance.teams.Any((t) => { return t.PlayStatus == TeamStatus.Playing || t.PlayStatus == TeamStatus.Waiting; })) return;
			/*
			lock (objectsync)
			{
				foreach (var t in Instance.teams)
					if (t.PlayStatus == TeamStatus.Playing || t.PlayStatus == TeamStatus.Waiting)
						return;
			}
			*/
			cleaning = !cleaning;
		}
		public static void TryDeleteAllTeams()
		{
			Instance.teams.TryClear((t) => { return t.PlayStatus != TeamStatus.Playing && t.Game == null; });
			/*
			var del = new List<Team>();
			lock (objectsync)
			{
				foreach (var t in Instance.teams)
					if (t.PlayStatus != TeamStatus.Playing && t.Game == null)
						del.Add(t);
				foreach (var t in del)
					Instance.teams.Remove(t);
				Instance.serialise("GoQuest.TryDeleteAllTeams", string.Empty);
			}
			*/
		}
		internal static bool TeamNameNotExists(string name, Team exception)
		{
			/*
			if ((Instance == null) || name.Equals(String.Empty))
				return false;
			lock (objectsync)
			{
				foreach (var t in Instance.teams)
					if (t != exception && t.Name.Equals(name))
						return false;
			*/
			return true;
			//}
		}
		internal static Team GetTeamByPin(short pin, Team exception)
		{
			/*
			if (Instance == null || pin == 0)
				return null;
			lock (objectsync)
			{
				foreach (var t in Instance.teams)
					if (t != exception && t.PinCode == pin)
						return t;
			*/
			return null;
			//}
		}
		public static bool CheckApprovedPins(ushort pin)
		{
			return Instance.teampins.ForEach_ReturnOnSuccess((p) => { return p == pin; });
			/*
			lock (objectsync)
			{
				foreach (var p in Instance.teampins)
					if (p == pin)
						return true;
			}
			*/
		}
		internal static void ModifyGame(GameVersion orgGame, GameVersion newGame)
		{
			/*
			lock (objectsync)
			{
				foreach (var g in Instance.gameversions)
					if (g == orgGame)
					{
						g.Modify(newGame);
						sql.AddGameVersion(g);
						Instance.serialise("GoQuest.ModifyGame", g.Name);
					}
			}
			*/
		}
		internal static void ResetGame(GameVersion gv)
		{
			/*
			lock (objectsync)
			{
				foreach (var g in Instance.gameversions)
					if (g == gv)
					{
						g.Reset();
						break;
					}
				Instance.serialise("GoQuest.ResetGame", gv.Name);
			}
			*/
		}
		public static string FormatVersionNo()
		{
			return String.Format("{0}.{1}.{2}{3}{4}", MAJOR_REV, MINOR_REV, RELEASE_REV, String.IsNullOrEmpty(REV_SUFFIX) ? "" : ".", REV_SUFFIX);
		}
		public static string FormatDBStatus()
		{
			StringBuilder sb = new StringBuilder();
			/*
			var values = GetValues(new DBErrors());
			foreach (var v in values)
				if ((dbfault & ((uint)(DBErrors)v)) > 0)
					sb.Append(v.ToString()).Append(" ");
			*/
			return sb.Length > 0 ? sb.ToString() : "None";
		}
		internal static IEnumerable<Enum> GetValues(Enum e)
		{
			List<Enum> list = new List<Enum>();
			Type type = e.GetType();
			foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.Static | BindingFlags.Public))
				list.Add((Enum)fieldInfo.GetValue(e));
			return list;
		}
	}
}
