using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

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
		internal bool Contains(T elem) { lock (this) return list.Contains(elem); }
		internal bool Remove(T elem) { lock (this) return list.Remove(elem); }
		internal void ForEach(Action<T> act) { lock (this) foreach (var t in list) act(t); }
		internal bool Any(Func<T, bool> pred) { lock (this) { return Where(pred).Count() > 0; } }
		internal IEnumerable<T> Where(Func<T, bool> pred) { lock (this) return list.Where(pred); }
		internal T FirstOrDefault(Func<T, bool> pred) { lock (this) return list.Where(pred).FirstOrDefault(); }
		internal void print() { lock (this) foreach (var v in list) StdOut.WriteLine(v.ToString()); }
	}
	internal class SafeGameVersionsList : SafeList<GameVersion>
	{
		internal GameVersion ClaimFirstEmpty(List<GameVersion> played)
		{
			lock (list)
			{
				var empties = Where(x => x.PlayState == GameState.EMPTY).ToList();
				var intersect = empties.Intersect(played);
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
		private static readonly byte MAJOR_REV = 0, MINOR_REV = 0, RELEASE_REV = 1;
		private static readonly string REV_SUFFIX = "";
		private static GoQuest instance = null;
		private GamesInterface gif = new GamesInterface();
		internal static bool cleaning;
		internal ushort ButtonHoldTime { get; set; } = 1000;    //todo
		internal ushort TempFlashPeriod { get; set; } = 500; //todo
		internal ushort DefaultTeamMins { get; set; } = 75; //todo
		internal ushort PinLength { get; set; } = 3; //todo
		[JsonProperty] internal SafeList<Team> teams;
		[JsonProperty] internal SafeGameVersionsList gameversions;
		[JsonIgnore] public static string Path { get; set; }
		[JsonProperty] private List<UserControlPanel> userpanels;
		[JsonIgnore] public static GoQuest Instance { get { return instance == null ? instance = deserialise() : instance; } }
		[JsonIgnore] public static ushort cpuload;
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
		public static void Initialise(object arg)
		{
			foreach (var p in Instance.userpanels)
				p.Initialise(arg, Instance);
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
			Instance.teams.Add(team);
			/*
			lock (objectsync)
			{
				instance.teams.Add(team);
				sql.AddTeam(team);
				instance.serialise("GoQuest.AddTeam", team.Name);
			}
			*/
		}
		public static bool AddTeam(string name)
		{
			bool ret = instance.teams.TryAdd(new Team(name, 0));
			/*
			lock (objectsync)
			{
				foreach (var t in instance.teams)
					if (t.Name.Equals(name))
						return false;
				var team = new Team(name);
				instance.teams.Add(team);
				sql.AddTeam(team);
				instance.serialise("GoQuest.AddTeam", name);
				return true;
			}
			*/
			instance.serialise();
			return ret;
		}
		internal static void ModifyTeam(Team orgTeam, Team newTeam)
		{
			/*
			lock (objectsync)
			{
				foreach (var t in instance.teams)
					if (t == orgTeam)
					{
						t.Modify(newTeam);
						sql.ModifyTeam(t);
						instance.serialise("GoQuest.ModifyTeam", t.Name);
					}
			}
			*/
		}
		internal static bool DeleteTeam(Team team)
		{
			/*
			lock (objectsync)
			{
				foreach (var t in instance.teams)
					if (t == team && t.PlayStatus != TeamStatus.Playing && t.Game == null)
					{
						instance.teams.Remove(t);
						instance.serialise("GoQuest.DeleteTeam", t.Name);
						return true;
					}
			}
			*/
			return false;
		}
		public static void ToggleCleaning()
		{
			if (instance.teams.Any((t) => { return t.PlayStatus == TeamStatus.Playing || t.PlayStatus == TeamStatus.Waiting; })) return;
			/*
			lock (objectsync)
			{
				foreach (var t in instance.teams)
					if (t.PlayStatus == TeamStatus.Playing || t.PlayStatus == TeamStatus.Waiting)
						return;
			}
			*/
			cleaning = !cleaning;
		}
		public static void TryDeleteAllTeams()
		{
			/*
			var del = new List<Team>();
			lock (objectsync)
			{
				foreach (var t in instance.teams)
					if (t.PlayStatus != TeamStatus.Playing && t.Game == null)
						del.Add(t);
				foreach (var t in del)
					instance.teams.Remove(t);
				instance.serialise("GoQuest.TryDeleteAllTeams", string.Empty);
			}
			*/
		}
		internal static bool TeamNameNotExists(string name, Team exception)
		{
			/*
			if ((instance == null) || name.Equals(String.Empty))
				return false;
			lock (objectsync)
			{
				foreach (var t in instance.teams)
					if (t != exception && t.Name.Equals(name))
						return false;
			*/
			return true;
			//}
		}
		internal static Team GetTeamByPin(short pin, Team exception)
		{
			/*
			if (instance == null || pin == 0)
				return null;
			lock (objectsync)
			{
				foreach (var t in instance.teams)
					if (t != exception && t.PinCode == pin)
						return t;
			*/
			return null;
			//}
		}
		public static bool CheckApprovedPins(ushort pin)
		{
			/*
			lock (objectsync)
			{
				foreach (var p in instance.teampins)
					if (p == pin)
						return true;
			}
			*/
			return false;
		}
		internal static void ModifyGame(GameVersion orgGame, GameVersion newGame)
		{
			/*
			lock (objectsync)
			{
				foreach (var g in instance.gameversions)
					if (g == orgGame)
					{
						g.Modify(newGame);
						sql.AddGameVersion(g);
						instance.serialise("GoQuest.ModifyGame", g.Name);
					}
			}
			*/
		}
		internal static void ResetGame(GameVersion gv)
		{
			/*
			lock (objectsync)
			{
				foreach (var g in instance.gameversions)
					if (g == gv)
					{
						g.Reset();
						break;
					}
				instance.serialise("GoQuest.ResetGame", gv.Name);
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
