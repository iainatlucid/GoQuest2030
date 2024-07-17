using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Lucid.GoQuest
{
	public class SafeList<T>
	{
		[JsonProperty] private List<T> list;
		[JsonIgnore] public int Count { get { return list.Count; } }
		public SafeList() { list = new List<T>(); }
		public void Add(T elem) { lock (this) list.Add(elem); }
		public bool Contains(T elem) { lock (this) return list.Contains(elem); }
		public bool Remove(T elem) { lock (this) return list.Remove(elem); }
		public IEnumerable<T> Where(Func<T, bool> pred) { lock (this) return list.Where(pred); }
		public T FirstOrDefault(Func<T, bool> pred) { lock (this) return list.Where(pred).FirstOrDefault(); }

		public void print() { return; lock (this) foreach (var v in list) Console.WriteLine(v); }
	}
	public class GoQuest2030
	{
		private static readonly byte MAJOR_REV = 0, MINOR_REV = 0, RELEASE_REV = 1;
		private static readonly string REV_SUFFIX = "";
		private static GoQuest2030 instance = null;
		[JsonIgnore] public static GoQuest2030 Instance { get { return instance == null ? instance = deserialise() : instance; } }
		[JsonProperty] private SafeList<Team> teams;
		[JsonProperty] internal SafeList<Game> games;
		public static TeamSequencer Sequencer = new TeamSequencer();
		private GoQuest2030() { }
		public GoQuest2030 detokenise()
		{
			return this;
		}
		private static GoQuest2030 deserialise()
		{
			string s;
			using (var file = new StreamReader(new FileStream(Directory.GetCurrentDirectory() + @"\..\..\..\goquest.json", FileMode.Open)))
				s = file.ReadToEnd();
			return JsonConvert.DeserializeObject<GoQuest2030>(s, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto }).detokenise();
		}
		private void tokenise()
		{
		}
		private void serialise()
		{
			tokenise();
			var s = JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
			if (File.Exists(Directory.GetCurrentDirectory() + @"\..\..\..\goquest.json"))
			{
				try { File.Delete(Directory.GetCurrentDirectory() + @"\..\..\..\goquest.json"); }
				catch (Exception e) { Console.WriteLine("EXCEPTION: serialise(): Cannot delete file: \r\n{0}", e.StackTrace); }
			}
			using (var file = File.CreateText(Directory.GetCurrentDirectory() + @"\..\..\..\goquest.json"))
				file.Write(s);
			detokenise();
		}
		public static void print()
		{
			Console.WriteLine(JsonConvert.SerializeObject(Instance, Formatting.Indented));
		}
	}
}
