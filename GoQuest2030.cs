using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

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
		public T this[int key] { get => list[key]; set => list[key] = value; }
		[JsonIgnore] public int Count { get { return list.Count; } }
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
				var empties = Where(x => x.State == State.EMPTY).ToList();
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
		[JsonIgnore] public static string Path { get; set; }
		[JsonIgnore] public static GoQuest Instance { get { return instance == null ? instance = deserialise() : instance; } }
		[JsonProperty] internal SafeList<Team> teams;
		[JsonProperty] internal SafeGameVersionsList gameversions;
		private GamesInterface gif = new GamesInterface();
		public static bool cleaning;
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
		private void serialise()
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
		public static void Print()
		{
			StdOut.WriteLine(JsonConvert.SerializeObject(Instance, Formatting.Indented));
		}
		private GoQuest init()
		{
			gameversions.AddDelegates(gif);
			return this;
		}
		public static bool AddTeam(string name)
		{
			bool ret = instance.teams.TryAdd(new Team(name));
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
		/*
		public static void TryDeleteAllTeams()
		{
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
		}
		*/
		public static IEnumerable<Enum> GetValues(Enum e)
		{
			List<Enum> list = new List<Enum>();
			Type type = e.GetType();
			foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.Static | BindingFlags.Public))
				list.Add((Enum)fieldInfo.GetValue(e));
			return list;
		}
	}
}
