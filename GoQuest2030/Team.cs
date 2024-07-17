using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Lucid.GoQuest
{
	internal class Team : Base
	{
		internal static int TeamsFinished = 0;
		private int id;
		internal int Played { get { return played.Count; } }
		internal Team() { }
		internal Team(string n, int i) { name = n; id = i; }
		private List<Game> played = new List<Game>();
		private Thread autoplay;
		internal void Autoplay() { autoplay = new Thread(play); autoplay.Start(); }
		private void play(object o)
		{
			//lock (GoQuest2030.Instance.games)
			{
				//var games = GoQuest2030.Instance.games.Where(x => x.State == State.EMPTY).ToList();
				//Console.Write("{0}{1}.",games.Count,name);
				//var intersect = games.Intersect(played);
				//games.RemoveAll(x => intersect.Contains(x));
				Game g;
				if ((g=GoQuest2030.Instance.games.ClaimFirstEmpty(played))!=null)
				{
					played.Add(g);
					g.Play(this, id, play);
				}
				else if (played.Count == GoQuest2030.Instance.games.Count)
					Console.WriteLine("***********************************************FINISHED {0}, total finished={1}", name, ++TeamsFinished);
				else
				{
					Thread.Sleep(100);
					play(o);
				}
			}
		}
	}
}
