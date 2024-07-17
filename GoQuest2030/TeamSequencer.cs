using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lucid.GoQuest
{
	public class TeamSequencer
	{
		[JsonProperty] private readonly int TOTAL_TEAMS = 50;
		private List<Thread> teams;
		public void Start() 
		{
			for (int i = 0; i < TOTAL_TEAMS; i++)
			{
				var t = new Team(i.ToString(), i);
				t.Autoplay();
			}
		}
	}
}
