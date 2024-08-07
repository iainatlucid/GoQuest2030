using Newtonsoft.Json;
using System.Threading;

namespace Lucid.GoQuest
{
	public static class AutoPlayer
	{
		private static TeamSequencer seq;
		public static void Start()
		{
			seq = new TeamSequencer();
			seq.Start();
		}
	}
	public class TeamSequencer
	{
		[JsonProperty] private readonly int TEAMS = 25;
		private Thread start;
		public void Start() { start = new Thread(run); start.Start(); }
		private void run()
		{
			while (true)
			{
				if (Team.TeamsInPlay == 0)
					for (int i = 1; i <= TEAMS; i++)
					{
						var t = new Team(i.ToString(), i);
						t.Autoplay();
						if (i % 10 == 0) Thread.Sleep(1000);
					}
				Thread.Sleep(500);
			}
		}
	}
}