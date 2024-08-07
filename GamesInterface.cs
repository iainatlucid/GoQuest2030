using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Lucid.GoQuest
{
	public delegate void JsonAction(JToken args);
	internal class GamesInterface
	{
		private TcpClient tcp;
		private Thread thread;
		private readonly List<JsonAction> jsonActions = new List<JsonAction>();
		private Dictionary<string, List<object>> elems;
		private volatile bool valid;
		internal Dictionary<string, Action> GameStarts { get; set; }
		internal Dictionary<string, Action> SuperQuests { get; set; }
		internal Dictionary<string, Action> GameNames { get; set; }
		internal GamesInterface()
		{
			elems = new Dictionary<string, List<object>>();
			GameStarts = new Dictionary<string, Action>();
			SuperQuests = new Dictionary<string, Action>();
			GameNames = new Dictionary<string, Action>();
			foreach (string line in File.ReadLines(GoQuest2030.Path + "jsonactions.txt"))
			{
				var equals = line.Split('=');
				if (equals.Length <= 1) continue;
				var slashes = equals[0].Split('/');
				var objects = new List<object>();
				foreach (var s in slashes)
					try { objects.Add(int.Parse(s)); }
					catch { objects.Add(s); }
				elems.Add(equals[1], objects);
			}
			jsonActions.Add(gameStart);
			jsonActions.Add(superQuest);
			jsonActions.Add(gameName);
			thread = new Thread(run);
			thread.Start();
		}
		private void gameStart(JToken token)
		{
			//StdOut.WriteLine("GameStart {0}", token.ToString());
			try
			{
				var values = token.Value<JArray>();
				foreach (var v in values)
					GameStarts[v.ToString()]();
			}
			catch (Exception e) { StdOut.WriteStr(e.Message); }
		}
		private void superQuest(JToken token)
		{
			//StdOut.WriteLine("SuperQuest {0}", token.ToString());
			try
			{
				var values = token.Value<JArray>();
				foreach (var v in values)
					SuperQuests[v.ToString()]();
			}
			catch (Exception e) { StdOut.WriteStr(e.Message); }
		}
		private void gameName(JToken token)
		{
			//StdOut.WriteLine("GameName {0}", token.ToString());
		}
		private void poll(object ns)
		{
			byte[] bytes = Encoding.GetEncoding(28591).GetBytes("{}");
			//int linebreaker = 0;
			try
			{
				while (true)
				{
					((NetworkStream)ns).Write(bytes, 0, 2);
					Thread.Sleep(1000);
					//if (linebreaker++ == 10)
					//{
					//	Console.WriteLine("Closing receiver...");
					//	tcp.Close();
					//	linebreaker = 0;
					//}

				}
			}
			catch { valid = false; }
		}
		public void AddAction(JsonAction action)
		{
			if (jsonActions.Contains(action)) return;
			jsonActions.Add(action);
		}
		private void run()
		{
			while (true)
			{
				var tl = new TcpListener(IPAddress.Any, 12345);
				tl.Start();
				tcp = tl.AcceptTcpClient();
				var ns = tcp.GetStream();
				tl.Stop();
				new Thread(poll).Start(ns);
				var sr = new StreamReader(ns, Encoding.GetEncoding(28591));
				var jr = new JsonTextReader(sr);
				jr.SupportMultipleContent = true;
				valid = true;
				while (valid)
					try
					{
						jr = new JsonTextReader(sr);
						jr.SupportMultipleContent = true;
						while (jr.Read())
						{
							var tokens = new JsonSerializer().Deserialize<JToken>(jr);
							foreach (var j in elems)
							{
								var t = tokens;
								foreach (var v in j.Value)
								{
									t = t[v];
									if (t == null)
										goto skip;
								}
								jsonActions.Where(a => a.Method.Name.Equals(j.Key)).First()(t);
							skip:;
							}
						}
					}
					catch (Exception e)
					{
						//Console.WriteLine("GamesInterface::Read: '{0}'", e.Message);
						sr.Dispose();
						ns.Dispose();
						tcp.Dispose();
					}
			}
		}
	}
}