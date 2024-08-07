using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
		[JsonIgnore] public int Count { get { return list.Count; } }
		internal SafeList() { list = new List<T>(); }
		internal void Add(T elem) { lock (this) list.Add(elem); }
		internal bool Contains(T elem) { lock (this) return list.Contains(elem); }
		internal bool Remove(T elem) { lock (this) return list.Remove(elem); }
		internal IEnumerable<T> Where(Func<T, bool> pred) { lock (this) return list.Where(pred); }
		internal T FirstOrDefault(Func<T, bool> pred) { lock (this) return list.Where(pred).FirstOrDefault(); }
		internal void print() { return; lock (this) foreach (var v in list) StdOut.WriteLine(v.ToString()); }
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
	public class GoQuest2030
	{
		private static readonly byte MAJOR_REV = 0, MINOR_REV = 0, RELEASE_REV = 1;
		private static readonly string REV_SUFFIX = "";
		private static GoQuest2030 instance = null;
		[JsonIgnore] public static string Path { get; set; }
		[JsonIgnore] public static GoQuest2030 Instance { get { return instance == null ? instance = deserialise() : instance; } }
		[JsonProperty] private SafeList<Team> teams;
		[JsonProperty] internal SafeGameVersionsList games;
		private GamesInterface gif = new GamesInterface();
		private GoQuest2030() { }
		private GoQuest2030 detokenise()
		{
			return this;
		}
		private static GoQuest2030 deserialise()
		{
			string s;
			using (var file = new StreamReader(new FileStream(Path + "goquest.json", FileMode.Open)))
				s = file.ReadToEnd();
			return JsonConvert.DeserializeObject<GoQuest2030>(s, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto }).detokenise().init();
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
		private GoQuest2030 init()
		{
			games.AddDelegates(gif);
			return this;
		}
	}
}
