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
		internal GamesInterface()
		{
			elems = new Dictionary<string, List<object>>();
			foreach (string line in File.ReadLines(Directory.GetCurrentDirectory() + @"\..\..\..\jsonactions.txt"))
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
			thread = new Thread(read);
			thread.Start();
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
			catch {valid = false; }
		}
		public void AddAction(JsonAction action)
		{
			if (jsonActions.Contains(action)) return;
			jsonActions.Add(action);
		}
		private void read()
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