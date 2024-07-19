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
		private Thread start;
		public void Start() { start = new Thread(run); start.Start(); }
		private void run()
		{
			for (int i = 1; i <= TOTAL_TEAMS; i++)
			{
				var t = new Team(i.ToString(), i);
				t.Autoplay();
				if (i % 10 == 0) Thread.Sleep(1000);
			}
		}
	}
}
