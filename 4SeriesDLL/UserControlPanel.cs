//#if false
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
/*
using Crestron.SimplSharp.Reflection;
using System.Collections;
using Crestron.SimplSharpPro.CrestronThread;
using System.Text;
using Crestron.SimplSharp;
*/
namespace Lucid.GoQuest
{
	public class UserControlPanel
	{
		private GoQuest host;
		private uint ipid;
		public uint Ipid { get { return ipid; } set { ipid = value; } }
		private SubpageJoin subpage;
		public SubpageJoin Subpage { get { return subpage; } set { subpage = value; } }
		private Page page;

		private Dictionary<SubpageJoin, Type> subpagetypes = new Dictionary<SubpageJoin, Type>()
		{
			{SubpageJoin.None,typeof(NoSubpage)},
			{SubpageJoin.EditTeam,typeof(EditTeam)},
			{SubpageJoin.ScoreCard,typeof(ScoreCard)},
			{SubpageJoin.EditGame,typeof(EditGame)},
		};

		private const string sgdfile = @"/user/goquest/UserControlXpanel.sgd";
		private ControlSystem ctrlsys;

		private StringCache stringcache, sostringcache1, sostringcache2;
		private UShortCache ushortcache, soushortcache1, soushortcache2;
		private BoolCache boolcache;

		[JsonIgnore]
		public XpanelForSmartGraphics panel;

		//private List<byte> enteredPin;
		//private string gamename;

		[JsonIgnore]
		public ControlSystem CtrlSys { get { return ctrlsys; } set { ctrlsys = value; } }
		public void Initialise(object ctrlsys, GoQuest gq)
		{
			host = gq;
			if (ipid == 0)
				return;

			//enteredPin = new List<byte>();
			panel = new XpanelForSmartGraphics(ipid, (ControlSystem)ctrlsys);
			panel.BaseEvent += new BaseEventHandler(panel_BaseEvent);
			//panel.ButtonStateChange += new ButtonEventHandler(panel_ButtonStateChange);
			panel.SigChange += new SigEventHandler(panel_SigChange);
			panel.OnlineStatusChange += new OnlineStatusChangeEventHandler(panel_OnlineStatusChange);
			if (panel.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
				StdOut.WriteLine("Xpanel Failed to register: {0}", panel.RegistrationFailureReason);
			else
			{
				panel.LoadSmartObjects(sgdfile);
				foreach (var smartObject in panel.SmartObjects)
					smartObject.Value.SigChange += new SmartObjectSigChangeEventHandler(smartobj_SigChange);
			}
			boolcache = new BoolCache(panel.BooleanInput);
			stringcache = new StringCache(panel.StringInput);
			ushortcache = new UShortCache(panel.UShortInput);
			sostringcache1 = new StringCache(panel.SmartObjects[1].StringInput);
			sostringcache2 = new StringCache(panel.SmartObjects[2].StringInput);
			soushortcache1 = new UShortCache(panel.SmartObjects[1].UShortInput);
			soushortcache2 = new UShortCache(panel.SmartObjects[2].UShortInput);
		}
		public void DatabaseOK(bool ok)
		{

		}
		private void smartobj_SigChange(GenericBase currentDevice, SmartObjectEventArgs args)
		{
			if (currentDevice != panel)
				return;
			try
			{
				StdOut.WriteLine("SO {0} Button {1} is {2}", args.SmartObjectArgs.ID, args.Sig.Number, args.Sig.BoolValue);
				page.Signal((ushort)args.SmartObjectArgs.ID, (ushort)args.Sig.Number, args.Sig);
			}
			catch (InvalidCastException e)
			{
				StdOut.WriteLine(">>> EXCEPTION: UserControlPanel.smartObj_SigChange: \r\n{0}", e.StackTrace);
			}
		}
		private void panel_OnlineStatusChange(GenericBase currentDevice, OnlineOfflineEventArgs args)
		{
			StdOut.WriteLine("Panel {0} is {1}line.", currentDevice.ID, args.DeviceOnLine ? "On" : "Off");
			//TODO: possible timing issue if a panel connection is cycled, rather than a processor reboot
			SetPage(SubpageJoin.None, false, null);
		}
		private void panel_SigChange(BasicTriList currentDevice, SigEventArgs args)
		{
			if ((currentDevice != panel) || (args.Sig.Number > 16000))
				return;
			//StdOut.WriteLine("{0} said {1}, {2} and {3}", args.Sig.Number, args.Sig.BoolValue, args.Sig.UShortValue, args.Sig.StringValue);
			try
			{
				page.Signal(0, (ushort)args.Sig.Number, args.Sig);
			}
			catch (InvalidCastException e)
			{
				StdOut.WriteLine(">>> EXCEPTION: UserControlPanel.panel_SigChange: \r\n{0}", e.StackTrace);
			}
		}
		//private void panel_ButtonStateChange(GenericBase device, ButtonEventArgs args)
		//{
		//	//Hard Keys, probably just use the SigChange
		//}

		private void panel_BaseEvent(GenericBase device, BaseEventArgs args)
		{
			// this bit is horribly complicated and probably not worth worrying about
		}
		public enum MainPageJoin : ushort
		{
			None = 0,
		}
		private class Page
		{
			protected UserControlPanel outer;
			protected GoQuest host;
			protected MainPageJoin main;
			protected SubpageJoin sub;
			protected Timer timeout;
			protected object userdata;

			public UserControlPanel Outer { get { return outer; } set { outer = value; } }
			public GoQuest Host { get { return host; } set { host = value; } }

			public Page()
			{
			}
			public virtual void Init(object o)
			{
				userdata = o;
			}
			protected void ResetTimeout()
			{
				if (timeout != null)
				{
					timeout.Change(10000, Timeout.Infinite);
					StdOut.WriteLine("{0} timeout RESET", this.GetType().Name);
				}
				else
				{
					timeout = new Timer(TimedOut, null, 10000, Timeout.Infinite);
					StdOut.WriteLine("{0} timeout START", this.GetType().Name);
				}
			}
			public void StopTimeout()
			{
				if (timeout != null)
				{
					timeout.Dispose();
					StdOut.WriteLine("{0} timeout STOPPED", this.GetType().Name);
				}
			}
			protected virtual void TimedOut(object o)
			{
				StdOut.WriteLine("{0} TIMED OUT", this.GetType().Name);
			}
			public void Show()
			{
				ResetTimeout();
				foreach (MainPageJoin m in GoQuest.GetValues(new MainPageJoin()))
					if ((ushort)m > 0)
						//outer.panel.BooleanInput[(ushort)m].BoolValue = (m == main);
						outer.boolcache.SetBool((ushort)m, m == main);
				foreach (SubpageJoin s in GoQuest.GetValues(new SubpageJoin()))
					if ((ushort)s > 0)
						//outer.panel.BooleanInput[(ushort)s].BoolValue = (s == sub);
						outer.boolcache.SetBool((ushort)s, s == sub);
			}
			public virtual void Signal(ushort smartID, ushort sigNum, Sig sig) { }
			public virtual void Update() { }
		}
		private class NoSubpage : Page
		{
			private Timer hold;
			private bool held = false;

			public NoSubpage()
			{
				main = MainPageJoin.None;
				sub = SubpageJoin.None;
			}
			public override void Init(object o)
			{
				base.Init(o);
			}
			private void Held(object o)
			{
				held = true;
				StdOut.WriteLine("{0} is held", ((Buttons)o).ToString());
				outer.panel.BooleanInput[(ushort)(Buttons)o].BoolValue = true;
			}
			public override void Signal(ushort smartID, ushort sigNum, Sig sig)
			{
				StdOut.WriteLine("smartID={0},join={1},val={2}", smartID, sigNum, sig.BoolValue);
				if (smartID > 0)
				{
					const int firstJoin = 4011;
					if (sigNum < firstJoin)
						return;
					//outer.panel.BooleanInput[sigNum].BoolValue = sig.BoolValue;
					if (!sig.BoolValue)
					{
						switch (smartID)
						{
							case 1:
								if ((sigNum - firstJoin) > host.teams.Count)
									return;
								try
								{
									StdOut.WriteLine("Team {0}", host.teams[sigNum - firstJoin].Name);
									outer.SetPage(SubpageJoin.EditTeam, false, host.teams[sigNum - firstJoin]); //crash - out of range
								}
								catch (Exception e) { StdOut.WriteLine(">>> EXCEPTION: UserControlPanel.NoSubpage.Signal - team: \r\n{0}", e.StackTrace); }
								break;
							case 2:
								if (((sigNum - firstJoin) / 10) > host.gameversions.Count)
									return;
								try
								{
									StdOut.WriteLine("Game {0}", host.gameversions[(sigNum - firstJoin) / 10].Name);
									outer.SetPage(SubpageJoin.EditGame, false, host.gameversions[(sigNum - firstJoin) / 10]);   //crash - out of range //this list in VTPRo has different incs
								}
								catch (Exception e) { StdOut.WriteLine(">>> EXCEPTION: UserControlPanel.NoSubpage.Signal - team: \r\n{0}", e.StackTrace); }
								break;
						}
					}
				}
				else
				{
					if (!sig.BoolValue)
					{
						switch (sigNum)
						{
							case (ushort)Buttons.AddTeam:
								GoQuest.AddTeam(String.Empty);
								break;
							case (ushort)Buttons.CleaningMode:
								GoQuest.ToggleCleaning();
								break;
							case (ushort)Buttons.DeleteAllTeams:
								if (hold != null)
									hold.Dispose();
								if (held)
									GoQuest.TryDeleteAllTeams();
								break;
								/*
								case (ushort)Buttons.Function3:
									if (hold != null)
										hold.Stop();
									if (held)
										GoQuest.ToggleDBCPUManual();
									break;
								case (ushort)Buttons.Function4:
									if (hold != null)
										hold.Stop();
									if (held)
										GoQuest.ToggleDBCPUOnly();
									break;
								*/
						}
						held = false;
						outer.panel.BooleanInput[sigNum].BoolValue = false;
					}
					else
					{
						switch (sigNum)
						{
							case (ushort)Buttons.DeleteAllTeams:
								hold = new Timer(Held, Buttons.DeleteAllTeams, GoQuest.Instance.ButtonHoldTime, Timeout.Infinite);
								break;
								/*
								case (ushort)Buttons.Function3:
									hold = new Timer(Held, Buttons.Function3, GoQuest.Instance.ButtonHoldTime);
									break;
								case (ushort)Buttons.Function4:
									hold = new Timer(Held, Buttons.Function4, GoQuest.Instance.ButtonHoldTime);
									break;
								*/
						}
					}
				}
			}
			public override void Update()
			{
				//outer.panel.StringInput[(ushort)Join.GameNameText].StringValue = host.Name;
				//outer.panel.StringInput[(ushort)Join.PointsValueText].StringValue = host.Score.ToString();
				outer.boolcache.SetBool((ushort)Buttons.CleaningMode, GoQuest.cleaning);
				//outer.boolcache.SetBool((ushort)Buttons.Function3, GoQuest.dbcpumanual);
				//outer.boolcache.SetBool((ushort)Buttons.Function4, GoQuest.dbcpuonly);
			}
		}
		private class EditPage : Page
		{
			protected Timer flash;
			protected byte flashField = 0;
			protected bool held = false;
			protected Timer hold;
			protected void FlashOff(object mask)
			{
				flashField &= (byte)((~((int)mask)) & 0xFF);
			}
		}
		private class EditTeam : EditPage
		{
			private Team newTeam, origTeam;

			public enum Buttons : ushort
			{
				Close = 10,
				Add,
				Reset,
				Clear,
				Default,
				Delete,
				Modify,
				Cert,
				TeamNameVisibility = 25,
				PinVisibility,
				StartVisibility,
				RemainVisibility,
				ScoreVisibility,
			}
			public enum Text : ushort
			{
				EditTeamDetails = 10,
				TeamName,
				Pin,
				TeamStatus,
				Start,
				Remain,
				Challenges,
				Score,
			}
			public EditTeam()
			{
				main = MainPageJoin.None;
				sub = SubpageJoin.EditTeam;
			}
			private void Held(object o)
			{
				held = true;
				outer.panel.StringInput[(ushort)Text.EditTeamDetails].StringValue = "OK";
				new Timer(SetEditTeamDetails, null, GoQuest.Instance.TempFlashPeriod, Timeout.Infinite); //todo
			}
			public override void Init(object o)
			{
				base.Init(o);
				outer.panel.BooleanInput[(ushort)Buttons.TeamNameVisibility].BoolValue = true;
				outer.panel.BooleanInput[(ushort)Buttons.PinVisibility].BoolValue = true;
				outer.panel.BooleanInput[(ushort)Buttons.RemainVisibility].BoolValue = true;
				outer.panel.BooleanInput[(ushort)Buttons.StartVisibility].BoolValue = true;

				origTeam = (Team)userdata;
				newTeam = new Team(origTeam);
				DisplayTeam(newTeam);
				flash = new Timer(Flash, null, 0, 250);
			}
			private void DisplayTeam(Team team)
			{
				outer.panel.StringInput[(ushort)Text.EditTeamDetails].StringValue = "Add, Edit, Delete Team";
				outer.panel.StringInput[(ushort)Text.TeamName].StringValue = team.Name;
				outer.panel.StringInput[(ushort)Text.Pin].StringValue = team.PinCode == 0 ? "----" : team.PinCode.ToString();
				outer.panel.StringInput[(ushort)Text.TeamStatus].StringValue = team.PlayStatus.ToString();
				outer.panel.StringInput[(ushort)Text.Start].StringValue = team.StartTime.Year == 9999 ?
					"--:--" : team.StartTime.ToString("HH:mm:ss");
				outer.panel.StringInput[(ushort)Text.Remain].StringValue = team.RemainingShortTimeString(false);
				outer.panel.StringInput[(ushort)Text.Challenges].StringValue = String.Format("{0}", /*team.GamesWon.Count.ToString(),*/ team.GamesTried.Count.ToString());
				outer.panel.StringInput[(ushort)Text.Score].StringValue = team.Score.ToString();
			}
			private void SetEditTeamDetails(object o)
			{
				outer.panel.StringInput[(ushort)Text.EditTeamDetails].StringValue = "Add, Edit, Delete Team";
			}
			public override void Signal(ushort smartID, ushort sigNum, Sig sig)
			{
				if (smartID > 0)
					return;

				switch (sig.Type)
				{
					case eSigType.Bool:

						//if (!(sigNum == (ushort)Buttons.Add || sigNum == (ushort)Buttons.Modify || sigNum == (ushort)Buttons.Delete))
						//outer.panel.BooleanInput[sigNum].BoolValue = sig.BoolValue;
						if (!sig.BoolValue)
						{
							ResetTimeout();
							switch (sigNum)
							{
								case (ushort)Buttons.Close:
									flashField = 0;
									outer.SetPage(SubpageJoin.None, false, null);
									break;
								case (ushort)Buttons.Add:
									if (hold != null)
										hold.Dispose();
									if (!held)
									{
										outer.panel.StringInput[(ushort)Text.EditTeamDetails].StringValue = "Hold button down until OK";
										new Timer(SetEditTeamDetails, null, GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
									}
									else if (newTeam.Name == string.Empty || newTeam.PinCode == 0)
									{
										outer.panel.StringInput[(ushort)Text.EditTeamDetails].StringValue = "Team name or PIN empty";
										new Timer(SetEditTeamDetails, null, GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
										flashField |= (1 << 0);
										flashField |= (1 << 1);
										new Timer(FlashOff, (1 << 0), GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
										new Timer(FlashOff, (1 << 1), GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
									}
									else if (GoQuest.TeamNameNotExists(newTeam.Name, null))
									{
										if (GoQuest.GetTeamByPin((short)newTeam.PinCode, null) == null)
										{
											if (flashField == 0)
											{
												//newTeam.GamesWon = new List<string>();
												GoQuest.AddTeam(newTeam);
												outer.SetPage(SubpageJoin.None, false, null);
											}
										}
										else
										{
											flashField |= (1 << 1);
											outer.panel.StringInput[(ushort)Text.EditTeamDetails].StringValue = "PIN exists";
											new Timer(SetEditTeamDetails, null, GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
											new Timer(FlashOff, (1 << 1), GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
										}
									}
									else
									{
										flashField |= (1 << 0);
										outer.panel.StringInput[(ushort)Text.EditTeamDetails].StringValue = "Team name exists";
										new Timer(SetEditTeamDetails, null, GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
										new Timer(FlashOff, (1 << 0), GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
									}
									break;
								case (ushort)Buttons.Reset:
									if (hold != null)
										hold.Dispose();
									if (!held)
									{
										outer.panel.StringInput[(ushort)Text.EditTeamDetails].StringValue = "Hold button down until OK";
										new Timer(SetEditTeamDetails, null, GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
									}
									else
									{
										newTeam.Game = null;
										GoQuest.ModifyTeam(origTeam, newTeam);
										StdOut.WriteLine("{0} RESET by USER: EditTeam", newTeam.Name);
										outer.SetPage(SubpageJoin.None, false, null);
									}
									/*
									if (GoQuest.AssignRandomPin(newTeam) == 0)
									{
										outer.panel.StringInput[(ushort)Text.EditTeamDetails].StringValue = "Cannot generate PIN";
										new Timer(SetEditTeamDetails, GoQuest.Instance.TempFlashPeriod);
										flashField |= (1 << 1);
									}
									outer.panel.StringInput[(ushort)Text.Pin].StringValue = newTeam.PinCode == 0 ? "ERR" : newTeam.PinCode.ToString();
									*/
									break;
								case (ushort)Buttons.Clear:
									newTeam.Reset();
									newTeam.PinCode = 0;
									newTeam.Name = string.Empty;
									newTeam.PlayStatus = newTeam.TestStatus();
									DisplayTeam(newTeam);
									break;
								case (ushort)Buttons.Default:
									newTeam.Reset();
									newTeam.StartTime = DateTime.Now;
									newTeam.EndTime = newTeam.StartTime + new TimeSpan(GoQuest.Instance.DefaultTeamMins * 60 * TimeSpan.TicksPerSecond);
									newTeam.PlayStatus = newTeam.TestStatus();
									DisplayTeam(newTeam);
									break;
								case (ushort)Buttons.Cert:
									outer.SetPage(SubpageJoin.ScoreCard, false, newTeam);
									break;
								case (ushort)Buttons.Modify:
									if (hold != null)
										hold.Dispose();
									if (!held)
									{
										outer.panel.StringInput[(ushort)Text.EditTeamDetails].StringValue = "Hold button down until OK";
										new Timer(SetEditTeamDetails, null, GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
									}
									else if (newTeam.Name == string.Empty || newTeam.PinCode == 0)
									{
										outer.panel.StringInput[(ushort)Text.EditTeamDetails].StringValue = "Team name or PIN empty";
										new Timer(SetEditTeamDetails, null, GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
										flashField |= (1 << 0);
										flashField |= (1 << 1);
										new Timer(FlashOff, (1 << 0), GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
										new Timer(FlashOff, (1 << 1), GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
									}
									else if (GoQuest.TeamNameNotExists(newTeam.Name, origTeam))
									{
										if (GoQuest.GetTeamByPin((short)newTeam.PinCode, origTeam) == null)
										{
											if (flashField == 0)
											{
												GoQuest.ModifyTeam(origTeam, newTeam);
												outer.SetPage(SubpageJoin.None, false, null);
											}
										}
										else
										{
											flashField |= (1 << 1);
											outer.panel.StringInput[(ushort)Text.EditTeamDetails].StringValue = "PIN exists";
											new Timer(SetEditTeamDetails, null, GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
											new Timer(FlashOff, (1 << 1), GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
										}
									}
									else
									{
										flashField |= (1 << 0);
										outer.panel.StringInput[(ushort)Text.EditTeamDetails].StringValue = "Team name exists";
										new Timer(SetEditTeamDetails, null, GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
										new Timer(FlashOff, (1 << 0), GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
									}
									break;
								case (ushort)Buttons.Delete:
									if (hold != null)
										hold.Dispose();
									if (!held)
									{
										outer.panel.StringInput[(ushort)Text.EditTeamDetails].StringValue = "Hold button down until OK";
										new Timer(SetEditTeamDetails, null, GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
										return;
									}
									if (!GoQuest.DeleteTeam(origTeam))
									{
										outer.panel.StringInput[(ushort)Text.EditTeamDetails].StringValue = "Ensure GameOver & check Games.";
										new Timer(SetEditTeamDetails, null, GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
										return;
									}
									outer.SetPage(SubpageJoin.None, false, null);
									break;
							}
							held = false;
						}
						else
						{
							switch (sigNum)
							{
								case (ushort)Buttons.Add:
									hold = new Timer(Held, Buttons.Add, GoQuest.Instance.ButtonHoldTime, Timeout.Infinite);
									break;
								case (ushort)Buttons.Reset:
									hold = new Timer(Held, Buttons.Reset, GoQuest.Instance.ButtonHoldTime, Timeout.Infinite);
									break;
								case (ushort)Buttons.Modify:
									hold = new Timer(Held, Buttons.Modify, GoQuest.Instance.ButtonHoldTime, Timeout.Infinite);
									break;
								case (ushort)Buttons.Delete:
									hold = new Timer(Held, Buttons.Delete, GoQuest.Instance.ButtonHoldTime, Timeout.Infinite);
									break;
							}
						}
						break;
					case eSigType.String:
						//GoQuest.WriteConsole(0,0,"String={0}", sig.StringValue);
						switch (sigNum)
						{
							case (ushort)Text.TeamName:
								newTeam.Name = sig.StringValue;
								break;
							case (ushort)Text.Pin:
								try
								{
									if (sig.StringValue.Length != GoQuest.Instance.PinLength) throw new Exception();
									if ((newTeam.PinCode = ushort.Parse(sig.StringValue))
										< (int)(Math.Pow(10, GoQuest.Instance.PinLength - 1) + 0.5)) throw new Exception();
									if (!GoQuest.CheckApprovedPins(newTeam.PinCode))
										throw new Exception();
									FlashOff(1 << 1);
								}
								catch (Exception)
								{
									flashField |= (1 << 1);
									outer.panel.StringInput[(ushort)Text.EditTeamDetails].StringValue
										= string.Format("Approved pin {0} to {1}"
											, (int)(Math.Pow(10, GoQuest.Instance.PinLength - 1) + 0.5)
											, (int)(Math.Pow(10, GoQuest.Instance.PinLength) - 1 + 0.5));
									new Timer(SetEditTeamDetails, null, GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
								}
								break;
							case (ushort)Text.Start:
								try
								{
									var ts = newTeam.EndTime - newTeam.StartTime;
									newTeam.StartTime = DateTime.Parse(sig.StringValue);
									newTeam.EndTime = newTeam.StartTime + ts;
									FlashOff(1 << 2);
								}
								catch (Exception)
								{
									flashField |= (1 << 2);
									outer.panel.StringInput[(ushort)Text.EditTeamDetails].StringValue = "Colon delimited time";
									new Timer(SetEditTeamDetails, null, GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
								}
								break;
							case (ushort)Text.Remain:
								try
								{
									var ts = TimeSpan.Parse(sig.StringValue);
									switch (newTeam.PlayStatus)
									{
										case TeamStatus.Waiting:
											newTeam.EndTime = newTeam.StartTime + ts;
											break;
										case TeamStatus.Playing:
										case TeamStatus.GameOver:
											newTeam.EndTime = DateTime.Now + ts;
											break;

									}
									FlashOff(1 << 3);
								}
								catch (Exception)
								{
									flashField |= (1 << 3);
									outer.panel.StringInput[(ushort)Text.EditTeamDetails].StringValue = "Colon delimited time";
									new Timer(SetEditTeamDetails, null, GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
								}
								break;
							case (ushort)Text.Score:
								try
								{
									var sc = ushort.Parse(sig.StringValue);
									newTeam.Score = sc;
									FlashOff(1 << 4);
								}
								catch (Exception)
								{
									flashField |= (1 << 4);
									outer.panel.StringInput[(ushort)Text.EditTeamDetails].StringValue = "Score 0 or greater";
									new Timer(SetEditTeamDetails, null, GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
								}
								break;
						}
						break;
				}
			}
			public override void Update()
			{
				outer.panel.StringInput[(ushort)Text.TeamStatus].StringValue = newTeam.PlayStatus.ToString();
				outer.panel.StringInput[(ushort)Text.Challenges].StringValue = String.Format("{0}", /*newTeam.GamesWon.Count.ToString(),*/ newTeam.GamesTried.Count.ToString());
				//outer.panel.StringInput[(ushort)Text.Score].StringValue = newTeam.Score.ToString();
			}
			private void Flash(object o)
			{
				if ((flashField & (1 << 0)) != 0)
					outer.panel.BooleanInput[(ushort)Buttons.TeamNameVisibility].BoolValue = !outer.panel.BooleanInput[(ushort)Buttons.TeamNameVisibility].BoolValue;
				if (((flashField & (1 << 1)) != 0))
					outer.panel.BooleanInput[(ushort)Buttons.PinVisibility].BoolValue = !outer.panel.BooleanInput[(ushort)Buttons.PinVisibility].BoolValue;
				if (((flashField & (1 << 2)) != 0))
					outer.panel.BooleanInput[(ushort)Buttons.StartVisibility].BoolValue = ((flashField & (1 << 2)) == 0) || !outer.panel.BooleanInput[(ushort)Buttons.StartVisibility].BoolValue;
				if (((flashField & (1 << 3)) != 0))
					outer.panel.BooleanInput[(ushort)Buttons.RemainVisibility].BoolValue = ((flashField & (1 << 3)) == 0) || !outer.panel.BooleanInput[(ushort)Buttons.RemainVisibility].BoolValue;
				if (((flashField & (1 << 4)) != 0))
					outer.panel.BooleanInput[(ushort)Buttons.ScoreVisibility].BoolValue = ((flashField & (1 << 4)) == 0) || !outer.panel.BooleanInput[(ushort)Buttons.ScoreVisibility].BoolValue;
				flash.Change(250, Timeout.Infinite);
			}
		}
		private class ScoreCard : Page
		{
			public enum Buttons : ushort
			{
				Close = 10,
			}
			public enum Text : ushort
			{
				TeamName = 1,
				Date,
				Points,
				Won,
				Tried,
			}
			public ScoreCard()
			{
				main = MainPageJoin.None;
				sub = SubpageJoin.ScoreCard;
			}
			public override void Init(object o)
			{
				base.Init(o);
				DisplayTeam((Team)userdata);
			}
			private void DisplayTeam(Team team)
			{
				outer.panel.StringInput[(ushort)Text.TeamName].StringValue = team.Name;
				outer.panel.StringInput[(ushort)Text.Date].StringValue = DateTime.Now.ToLongDateString();
				outer.panel.StringInput[(ushort)Text.Points].StringValue = team.Score.ToString();
				//outer.panel.StringInput[(ushort)Text.Won].StringValue = String.Format("{0}", team.GamesWon.Count.ToString());
				outer.panel.StringInput[(ushort)Text.Tried].StringValue = String.Format("{0}", team.GamesTried.Count.ToString());
			}
			public override void Signal(ushort smartID, ushort sigNum, Sig sig)
			{
				if (smartID > 0)
					return;

				switch (sig.Type)
				{
					case eSigType.Bool:

						if (!sig.BoolValue)
						{
							ResetTimeout();
							switch (sigNum)
							{
								case (ushort)Buttons.Close:
									outer.SetPage(SubpageJoin.None, false, null);
									break;
							}
						}
						break;
				}
			}
			//			public override void Update()
			//			{
			//				outer.panel.StringInput[(ushort)Text.TeamStatus].StringValue = newTeam.PlayStatus.ToString();
			//				outer.panel.StringInput[(ushort)Text.Challenges].StringValue = String.Format("{0}/{1}", newTeam.GamesWon.Count.ToString(), newTeam.GamesTried.Count.ToString());
			//				outer.panel.StringInput[(ushort)Text.Score].StringValue = newTeam.Score.ToString();
			//			}
		}
		private class EditGame : EditPage
		{
			private GameVersion newGame, origGame;

			public enum Buttons : ushort
			{
				Close = 110,
				Enable,
				Next,
				Clear,
				Default,
				Disable,
				Modify,
				Reset,
				GameNameVisibility = 125,
				IdVisibility,
				PointsVisibility,
				SecondsVisibility,
			}
			public enum Text : ushort
			{
				EditGameDetails = 110,
				GameName,
				ID,
				GameStatus,
				Points,
				Seconds,
				Challenges,
				Score,
			}
			public EditGame()
			{
				main = MainPageJoin.None;
				sub = SubpageJoin.EditGame;
			}
			private void Held(object o)
			{
				held = true;
				outer.panel.StringInput[(ushort)Text.EditGameDetails].StringValue = "OK";
				new Timer(SetEditGameDetails, null, GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
			}
			public override void Init(object o)
			{
				base.Init(o);
				outer.panel.BooleanInput[(ushort)Buttons.GameNameVisibility].BoolValue = true;
				outer.panel.BooleanInput[(ushort)Buttons.IdVisibility].BoolValue = true;
				outer.panel.BooleanInput[(ushort)Buttons.PointsVisibility].BoolValue = true;
				outer.panel.BooleanInput[(ushort)Buttons.SecondsVisibility].BoolValue = true;

				origGame = (GameVersion)userdata;
				newGame = new GameVersion(origGame);
				DisplayGame(newGame);
				flash = new Timer(Flash, null, 250, Timeout.Infinite);
			}
			private void DisplayGame(GameVersion game)
			{
				outer.panel.StringInput[(ushort)Text.EditGameDetails].StringValue = "Enable, Modify, Disable Game";
				outer.panel.StringInput[(ushort)Text.GameName].StringValue = game.Name;
				outer.panel.StringInput[(ushort)Text.ID].StringValue = game.ID.ToString();
				outer.panel.StringInput[(ushort)Text.GameStatus].StringValue = game.State.ToString();
				outer.panel.StringInput[(ushort)Text.Points].StringValue = game.Score.ToString();
				outer.panel.StringInput[(ushort)Text.Seconds].StringValue = game.TimeAllowed.ToString();
				//outer.panel.StringInput[(ushort)Text.Challenges].StringValue = String.Format("{0}/{1}", team.GamesWon.Count.ToString(), team.GamesTried.Count.ToString());
				//outer.panel.StringInput[(ushort)Text.Score].StringValue = team.Score.ToString();
			}
			private void SetEditGameDetails(object o)
			{
				outer.panel.StringInput[(ushort)Text.EditGameDetails].StringValue = "Enable, Modify, Disable Game";
			}
			public override void Signal(ushort smartID, ushort sigNum, Sig sig)
			{
				if (smartID > 0)
					return;

				switch (sig.Type)
				{
					case eSigType.Bool:

						//if (!(sigNum == (ushort)Buttons.Add || sigNum == (ushort)Buttons.Modify || sigNum == (ushort)Buttons.Delete))
						//outer.panel.BooleanInput[sigNum].BoolValue = sig.BoolValue;
						if (!sig.BoolValue)
						{
							ResetTimeout();
							switch (sigNum)
							{
								case (ushort)Buttons.Close:
									flashField = 0;
									outer.SetPage(SubpageJoin.None, false, null);
									break;
								case (ushort)Buttons.Enable:
									if (hold != null)
										hold.Dispose();
									if (!held)
									{
										outer.panel.StringInput[(ushort)Text.EditGameDetails].StringValue = "Hold button down until OK";
										new Timer(SetEditGameDetails, null, GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
									}
									else
									{
										origGame.SetOnline();
										outer.SetPage(SubpageJoin.None, false, null);
									}
									break;
								case (ushort)Buttons.Next:
									break;
								case (ushort)Buttons.Clear:
									break;
								case (ushort)Buttons.Default:
									break;
								case (ushort)Buttons.Reset:
									if (hold != null)
										hold.Dispose();
									if (!held)
									{
										outer.panel.StringInput[(ushort)Text.EditGameDetails].StringValue = "Hold button down until OK";
										new Timer(SetEditGameDetails, null, GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
									}
									else
									{
										GoQuest.ResetGame(origGame);
										StdOut.WriteLine("{0} RESET by USER: EditGame", origGame.Name);
										outer.SetPage(SubpageJoin.None, false, null);
									}
									break;
								case (ushort)Buttons.Modify:
									if (hold != null)
										hold.Dispose();
									if (!held)
									{
										outer.panel.StringInput[(ushort)Text.EditGameDetails].StringValue = "Hold button down until OK";
										new Timer(SetEditGameDetails, null, GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
									}
									else if (newGame.Team != null)
									{
										outer.panel.StringInput[(ushort)Text.EditGameDetails].StringValue = "Team still playing game!";
										new Timer(SetEditGameDetails, null, GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
										flashField |= (1 << 2);
										new Timer(FlashOff, (1 << 2), GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
									}
									else if (newGame.Score == 0)
									{
										outer.panel.StringInput[(ushort)Text.EditGameDetails].StringValue = "Game may not score 0";
										new Timer(SetEditGameDetails, null, GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
										flashField |= (1 << 2);
										new Timer(FlashOff, (1 << 2), GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
									}
									else if (flashField == 0)
									{
										GoQuest.ModifyGame(origGame, newGame);
										outer.SetPage(SubpageJoin.None, false, null);
									}
									break;
								case (ushort)Buttons.Disable:
									if (hold != null)
										hold.Dispose();
									if (!held)
									{
										outer.panel.StringInput[(ushort)Text.EditGameDetails].StringValue = "Hold button down until OK";
										new Timer(SetEditGameDetails, null, GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);//todo
									}
									else
									{
										origGame.SetOffline();
										outer.SetPage(SubpageJoin.None, false, null);
									}
									break;
							}
							held = false;
						}
						else
						{
							switch (sigNum)
							{
								case (ushort)Buttons.Enable:
									hold = new Timer(Held, Buttons.Enable, GoQuest.Instance.ButtonHoldTime, Timeout.Infinite);
									break;
								case (ushort)Buttons.Modify:
									hold = new Timer(Held, Buttons.Modify, GoQuest.Instance.ButtonHoldTime, Timeout.Infinite);
									break;
								case (ushort)Buttons.Disable:
									hold = new Timer(Held, Buttons.Disable, GoQuest.Instance.ButtonHoldTime, Timeout.Infinite);
									break;
								case (ushort)Buttons.Reset:
									hold = new Timer(Held, Buttons.Reset, GoQuest.Instance.ButtonHoldTime, Timeout.Infinite);
									break;
							}
						}
						break;
					case eSigType.String:
						//GoQuest.WriteConsole(0,0,"String={0}", sig.StringValue);
						switch (sigNum)
						{
							case (ushort)Text.Points:
								try
								{
									if ((newGame.Score = byte.Parse(sig.StringValue)) <= 0) throw new Exception();
									FlashOff(1 << 2);
								}
								catch (Exception)
								{
									flashField |= (1 << 2);
									outer.panel.StringInput[(ushort)Text.EditGameDetails].StringValue = string.Format("Number must be 0 - 255");
									new Timer(SetEditGameDetails, null, GoQuest.Instance.TempFlashPeriod, Timeout.Infinite);
								}
								break;
						}
						break;
				}
			}
			public override void Update()
			{
				outer.panel.StringInput[(ushort)Text.GameStatus].StringValue = newGame.State.ToString();
				//outer.panel.StringInput[(ushort)Text.Challenges].StringValue = String.Format("{0}/{1}", newTeam.GamesWon.Count.ToString(), newTeam.GamesTried.Count.ToString());
				//outer.panel.StringInput[(ushort)Text.Score].StringValue = newTeam.Score.ToString();
			}
			private void Flash(object o)
			{
				if ((flashField & (1 << 0)) != 0)
					outer.panel.BooleanInput[(ushort)Buttons.GameNameVisibility].BoolValue = !outer.panel.BooleanInput[(ushort)Buttons.GameNameVisibility].BoolValue;
				if (((flashField & (1 << 1)) != 0))
					outer.panel.BooleanInput[(ushort)Buttons.IdVisibility].BoolValue = !outer.panel.BooleanInput[(ushort)Buttons.IdVisibility].BoolValue;
				if (((flashField & (1 << 2)) != 0))
					outer.panel.BooleanInput[(ushort)Buttons.PointsVisibility].BoolValue = ((flashField & (1 << 2)) == 0) || !outer.panel.BooleanInput[(ushort)Buttons.PointsVisibility].BoolValue;
				if (((flashField & (1 << 3)) != 0))
					outer.panel.BooleanInput[(ushort)Buttons.SecondsVisibility].BoolValue = ((flashField & (1 << 3)) == 0) || !outer.panel.BooleanInput[(ushort)Buttons.SecondsVisibility].BoolValue;
				flash.Change(250, Timeout.Infinite);
			}
		}
		public enum SubpageJoin : ushort
		{
			None = 0,
			ScoreCard = 8,
			EditTeam,
			EditGame = 109,
		}
		public enum Buttons : ushort
		{
			CleaningMode = 19,
			DeleteAllTeams,
			Function3,
			Function4,
			AddTeam = 52,
		}
		public enum Text : ushort
		{
			CpuLoad = 6,
			Version,
			Database,
		}
		public void Update()
		{
			try
			{
				stringcache.SetString((ushort)Text.CpuLoad, GoQuest.cpuload.ToString());
				stringcache.SetString((ushort)Text.Version, GoQuest.FormatVersionNo());
				stringcache.SetString((ushort)Text.Database, GoQuest.FormatDBStatus());
				ushortcache.SetUShort(3, GoQuest.cpuload, 0);   //TODO: Set this to Gauges.CpuLoad at next iteration

				uint N = (uint)(host.teams.Count);
				uint M = (uint)(host.gameversions.Count);

				panel.SmartObjects[1].UShortInput["Set Number of Items"].UShortValue = (ushort)host.teams.Count;
				panel.SmartObjects[2].UShortInput["Set Number of Items"].UShortValue = (ushort)host.gameversions.Count;

				lock (GoQuest.Instance.teams) //todo
				{
					for (uint i = 0; i < N; i++)
					{
						sostringcache1.SetString(i * 5 + 1 + 10, host.teams[(int)i].Name);
						sostringcache1.SetString(i * 5 + 2 + 10, host.teams[(int)i].PinCode.ToString());
						sostringcache1.SetString(i * 5 + 3 + 10,
							host.teams[(int)i].PlayStatus != TeamStatus.Playing ?
								host.teams[(int)i].PlayStatus.ToString() :
								host.teams[(int)i].Game == null ?
									"..MOVING.." :
									host.teams[(int)i].Game.Name.Length > 10 ? host.teams[(int)i].Game.Name.Substring(0, 10) : host.teams[(int)i].Game.Name);
						soushortcache1.SetUShort(i * 4 + 2 + 10, host.teams[(int)i].Score, 0);
						sostringcache1.SetString(i * 5 + 4 + 10, host.teams[(int)i]
							.PlayStatus == TeamStatus.Created ? "--:--" : host.teams[(int)i].StartTime.ToString("HH:mm:ss"));
						sostringcache1.SetString(i * 5 + 5 + 10, host.teams[(int)i].RemainingShortTimeString(false));
						soushortcache1.SetUShort(i * 4 + 1 + 10, (ushort)(host.teams[(int)i].JustFailed > 0 ? 4 : i < 3 ? i + 1 : 0), 0);
					}
				}
				lock (GoQuest.Instance.gameversions) //todo
				{
					for (uint i = 0; i < M; i++)
					{
						sostringcache2.SetString(i * 10 + 1 + 10, host.gameversions[(int)i].ID.ToString());
						sostringcache2.SetString(i * 10 + 2 + 10, host.gameversions[(int)i].Name);
						sostringcache2.SetString(i * 10 + 3 + 10,
							host.gameversions[(int)i].State == GameState.DISABLED ?
							"-DISABLED-" : host.gameversions[(int)i].Team != null ? host.gameversions[(int)i].Team.Name : "-----");
						sostringcache2.SetString(i * 10 + 4 + 10, host.gameversions[(int)i].Score.ToString());
						sostringcache2.SetString(i * 10 + 5 + 10, host.gameversions[(int)i].TimeAllowed.ToString());
						soushortcache2.SetUShort(i * 5 + 1 + 10,
							host.gameversions[(int)i].State == GameState.DISABLED ?
								(ushort)0 :
								host.gameversions[(int)i].State != GameState.PLAYING && host.gameversions[(int)i].LastPlayFailed ?
									(ushort)3 :
									host.gameversions[(int)i].State == GameState.PLAYING ?
										(ushort)1 :
										(ushort)2, 0);

					}
				}
				if (page != null)
					page.Update();
				else
					StdOut.WriteLine("page NULL at UserControlPanel");
			}
			catch (Exception e) { StdOut.WriteLine(">>> EXCEPTION: UserControlPanel.Update: \r\n{0}", e.StackTrace); }
		}
		public void SetPage(SubpageJoin j, bool serialise, object userdata)
		{
			if (page != null)
				page.StopTimeout();
			Type pagetype;
			subpagetypes.TryGetValue(j, out pagetype);
			if (pagetype != null)
			{
				page = (Page)Activator.CreateInstance(pagetype);
				page.Outer = this;
				page.Host = host;
				page.Init(userdata);
				page.Show();
				page.Update();
				Subpage = j;
				if (serialise)
					GoQuest.Instance.serialise();
			}
		}
	}
}
//#endif