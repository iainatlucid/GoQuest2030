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
		internal Thread thread;
		private readonly List<JsonAction> jsonActions = new List<JsonAction>();
		private Dictionary<string, List<object>> elems;
		internal GamesInterface()
		{
			elems = new Dictionary<string, List<object>>();
			jsonActions.Add(Mary);
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
		private void read()
		{
			var tl = new TcpListener(IPAddress.Any, 12345);
			tl.Start();
			var sk = tl.AcceptTcpClient();
			var ns = sk.GetStream();
			var sr = new StreamReader(ns, Encoding.GetEncoding(28591));
			var jr = new JsonTextReader(sr);
			jr.SupportMultipleContent = true;
			while (true)
				try
				{
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
					Console.WriteLine("GamesInterface::Read: '{0}'", e.Message);
					jr = new JsonTextReader(new StreamReader(ns, Encoding.GetEncoding(28591)));
					jr.SupportMultipleContent = true;
				}
		}
		private void Mary(JToken j)
		{
			Console.WriteLine("Mary {0}", j);
		}
	}
}