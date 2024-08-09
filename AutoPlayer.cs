using System.Threading;

namespace Lucid.GoQuest
{
	/*
	public static class AutoPlayer
	{
		private static TeamSequencer seq;
		public static void Start()
		{
			seq = new TeamSequencer();
			seq.Start();
		}
		public static void Stop() { seq.Quit = true; }
	}
	public class TeamSequencer
	{
		public bool Quit { get; set; }
		private readonly int TEAMS = 25;
		private Thread start;
		public void Start() { start = new Thread(run); start.Start(); }
		private void run()
		{
			while (!Quit)
			{
				if (Team.TeamsInPlay == 0)
					GoQuest.Instance.teams.ForEach((t) => t.Autoplay());
				Thread.Sleep(500);
			}
		}
	}
	*/
}