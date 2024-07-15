using Newtonsoft.Json;
using System;
using System.IO;

namespace Lucid.GoQuest
{
	public class GoQuest2030
	{
		private static GoQuest2030 instance = null;
		[JsonIgnore] public static GoQuest2030 Instance { get { return instance == null ? instance = deserialise() : instance; } }
		private GoQuest2030() 
		{
			Console.WriteLine(JsonConvert.SerializeObject(this));
		}
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
			Console.Write("Saving ... ");
			tokenise();
			var s = JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
			if (File.Exists(Directory.GetCurrentDirectory() + @"\..\..\..\goquest.json"))
			{
				try { File.Delete(Directory.GetCurrentDirectory() + @"\..\..\..\goquest.json"); }
				catch (Exception e) { Console.WriteLine("EXCEPTION: serialise(): Cannot delete file: \r\n{0}", e.StackTrace); }
			}
			using (var file = File.CreateText(Directory.GetCurrentDirectory() + @"\..\..\..\goquest.json"))
				file.Write(s);
			Console.WriteLine("done.");
			detokenise();
		}
	}
}
