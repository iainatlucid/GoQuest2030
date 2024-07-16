using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lucid.GoQuest
{
	public class TeamSequencer
	{
		[JsonProperty] private readonly int TOTAL_TEAMS = 10;
		private List<Thread> teams;
		public void Start() { Task.Run(() => run()); }
		private void run()
		{
			for (int i = 0; i < TOTAL_TEAMS; i++)
			{
				var t = new Team("Team " + i, i);
				t.Autoplay();
			}
		}
	}
}
