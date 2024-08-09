using Newtonsoft.Json;
using System;
using System.Threading;

namespace Lucid.GoQuest
{
	//Production
	internal enum GameState
	{
		EMPTY,
		OFFERED,
		CONFIRMED,
		PLAYING,
		OCCUPIED,
		EXITING,
		DISABLED
	}
	internal partial class GameVersion : Base
	{
		[JsonProperty] internal byte Score { get; set; }
		[JsonProperty] internal ushort TimeAllowed { get; set; }
		[JsonProperty] internal GameState State { get; set; }
		[JsonIgnore] internal Team Team { get; set; }
		[JsonIgnore] internal bool LastPlayFailed { get; set; }
		internal GameVersion() { }
		internal GameVersion(GameVersion g) { }
		internal void AddDelegates(GamesInterface gif)
		{
			gif.GameStarts.Add(Name, gameStart);
			gif.SuperQuests.Add(Name, superQuest);
			gif.GameNames.Add(Name, gameName);
		}
		private void gameStart()
		{
			StdOut.WriteLine("GAME STARTED for {0}", Name);
		}
		private void superQuest()
		{
			StdOut.WriteLine("SUPERQUEST called for {0}", Name);
		}
		private void gameName()
		{
		}
		public void SetOnline()
		{
			/*
			Reset();
			panel.SetPage(GamePanel.SubpageJoin.FrontPage, true);
			fusionio.GameEnabled(true);
			*/
		}
		public void SetOffline()
		{
			/*
			Reset();
			state = GameState.DISABLED;
			panel.SetPage(GamePanel.SubpageJoin.OutOfService, true);
			fusionio.GameEnabled(false);
			*/
		}
	}
	//Test
	internal partial class GameVersion : Base
	{
		public override string ToString() { return String.Format("{0}:{1}", Name, State); }
		internal volatile GameState PlayState = GameState.EMPTY;
		internal void Claim() { PlayState = GameState.PLAYING; }
		internal void Play(Team team)
		{
			Team = team;
			Console.WriteLine("'{0}' started playing {1}...", team.Name, Name);
			Thread.Sleep(1000); //playing...
			Console.WriteLine("'{0}' FINISHED {1}, played {2}.", team.Name, Name, team.GamesTried.Count + 1); ;
			Team = null;
			PlayState = GameState.EMPTY;	
		}
	}
}
