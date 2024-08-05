using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Sockets;
using System.Text;

namespace Lucid.GoQuest
{
	public class GoQuestJsonSender
	{
		private TcpClient tcp;
		private NetworkStream ns;
		private StreamWriter sw;
		private JObject jobj;
		private JsonTextWriter jw;
		private int presses;
		private Thread t;
		private volatile bool valid;
		public Action<ushort> OpenDoor { get; set; }
		public Action<ushort> GameControl { get; set; }
		public Action<ushort> DoorInhibit { get; set; }
		public Action<ushort> HintAvailable { get; set; }
		public Action<ushort> SuperQuestAvailable { get; set; }
		public Action<string> TeamName { get; set; }
		public Action<string> TeamScore { get; set; }
		public Action<ushort> RemainingSessionTime { get; set; }

		public GoQuestJsonSender()
		{
			Thread.Sleep(1000);
			t = new Thread(ping);
			t.Start();
		}
		private void ping(object o)
		{
			int state = 0;
			//int linebreaker = 0;
			while (true)
			{
				try
				{
					switch (state)
					{
						case 0:
							tcp = new TcpClient();
							state++;
							break;
						case 1:
							tcp.Connect("127.0.0.1", 12345);
							ns = tcp.GetStream();
							sw = new StreamWriter(ns, Encoding.GetEncoding(28591));
							jw = new JsonTextWriter(sw);
							jobj = new JObject();
							valid = true;
							state++;
							break;
						case 2:
							while (state > 0)
								try
								{
									jobj.WriteTo(jw);
									jobj = new JObject();
									//jobj.Add(new JProperty("Test", "icles"));
									jw.Flush();
									Thread.Sleep(1000);
									//if (linebreaker++ == 18)
									//{
									//	Console.WriteLine("Closing sender...");
									//	tcp.Close();
									//	linebreaker = 0;
									//}
								}
								catch (Exception e)
								{
									Console.WriteLine("GoQuestJsonSender::ping: '{0}'", e.Message);
									state = 0;
									sw.Dispose();
									ns.Dispose();
									tcp.Dispose();
								}
							break;
					}
				}
				catch { Thread.Sleep(1000); }
			}
		}
		private void send()
		{
			jobj.WriteTo(jw);
			jobj = new JObject();
			jw.Flush();
			presses = 0;
		}
		private JObject AddArray(string[] path, string key, object value, bool unique = true)
		{
			if (jobj != null)
			{
				var token = jobj;
				foreach (var p in path)
				{
					if (!token.ContainsKey(p))
						token.Add(p, new JObject());
					token = (JObject)token[p];
				}
				if (!token.ContainsKey(key))
					token.Add(key, new JArray());
				if (unique && !token[key].Any(x => { return JToken.DeepEquals(x, new JValue(value)); }))
					((JArray)token[key]).Add(value);
				return token;
			}
			return null;
		}
		private JObject AddProperty(string[] path, string key, object value)
		{
			if (jobj != null)
			{
				var token = jobj;
				foreach (var p in path)
				{
					if (!token.ContainsKey(p))
						token.Add(p, new JObject());
					token = (JObject)token[p];
				}
				if (!token.ContainsKey(key))
					token.Add(new JProperty(key, value));
				return token;
			}
			return null;
		}
		private JObject AddObject(string[] path)
		{
			if (jobj != null)
			{
				var token = jobj;
				foreach (var p in path)
				{
					if (!token.ContainsKey(p))
						token.Add(p, new JObject());
					token = (JObject)token[p];
				}
				return token;
			}
			return null;
		}
		public void GameName(object id) { AddObject(new string[] { "Jesus", "Built", "My", "Hotrod" }).Add(new JProperty("aaaa", "bisto")); }
		public void GameTimeTotal(object id) { AddObject(new string[] { "Jesus", "Built", "My", "Hotrod" }).Add(new JProperty("aaaa", "bisto")); }
		public void GameTimeRemaining(object id) { AddObject(new string[] { "Jesus", "Built", "My", "Hotrod" }).Add(new JProperty("aaaa", "bisto")); }
		public void GameStart(object id, bool hard = false) { AddProperty(new string[] { "Jesus", "Trashed", "My", "Hotrod" }, "GameStart", id); }
		public void Milestone(object id) { AddArray(new string[] { "Jesus", "Ate", "My", "Hotrod" }, "SuperQuest", id); }
		public void GameOver(object id) { AddArray(new string[] { "Jesus", "Ate", "My", "Hotrod" }, "SuperQuest", id); }
		public void Hint(object id) { AddArray(new string[] { "Jesus", "Ate", "My", "Hotrod" }, "SuperQuest", id); }
		public void Door(object id, bool closed) { AddArray(new string[] { "Jesus", "Ate", "My", "Hotrod" }, "SuperQuest", id); }
		public void SuperQuest(object id) { AddArray(new string[] { "Jesus", "Ate", "My", "Hotrod" }, "SuperQuest", id); }
		public void Release()
		{
			presses--;
			if (presses <= 0)
				send();
		}
	}
}