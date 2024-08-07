namespace Lucid.GoQuest
{
	[System.ComponentModel.DesignerCategory("")]
	public class MainForm : Form
	{
		private int GAMES = 30;
		private GoQuestJsonSender json;
		private List<string> gameNames = new List<string>
		{
				"Monty On The Run"
				,"Wizball"
				,"William Wobbler"
				,"Nemesis The Warlock"
				,"Thing On A Spring"
				,"Zoids"
				,"Suicide Express"
				,"Kikstart"
				,"Commando"
				,"Master Of Magic"
				,"Gauntlet"
				,"The Last V8"
				,"Atic Atac"
				,"Underwurlde"
				,"Jetpac"
				,"Paradroid"
				,"Sabre Wulf"
				,"The Hobbit"
				,"Lunar Jetman"
				,"Manic Miner"
				,"Jetset Willy"
				,"Bugsplat"
				,"Game Of Life"
				,"Adventure A: Planet Of Death"
				,"Fungaloids"
				,"Penetrator"
				,"Tranz-Am"
				,"Cookie"
				,"Schooldaze"
				,"Dracula"
			};
		public MainForm()
		{
			SuspendLayout();
			for (int i = 0; i < GAMES; i++)
			{
				var l = new Label();
				l.Text = string.Format("Game\n{0}", i + 1);
				l.Name = l.Text.ToLower();
				l.Location = new Point(110 * i, 10);
				l.Size = new Size(new Point(100, 80));
				Controls.Add(l);
				var b = new Button();
				b.Name = string.Format("play{0}", i + 1);
				b.Text = "Play";
				b.Location = new Point(110 * i, 100);
				b.Size = new Size(new Point(100, 80));
				b.UseVisualStyleBackColor = true;
				b.MouseDown += play_MouseDown;
				b.MouseUp += mouseUp;
				Controls.Add(b);
				b = new Button();
				b.Name = string.Format("super{0}", i + 1);
				b.Text = "Super";
				b.Location = new Point(110 * i, 200);
				b.Size = new Size(new Point(100, 80));
				b.UseVisualStyleBackColor = true;
				b.MouseDown += super_MouseDown;
				b.MouseUp += mouseUp;
				Controls.Add(b);
				b = new Button();
				b.Name = string.Format("name{0}", i + 1);
				b.Text = "Name";
				b.Location = new Point(110 * i, 300);
				b.Size = new Size(new Point(100, 80));
				b.UseVisualStyleBackColor = true;
				b.MouseDown += name_MouseDown;
				b.MouseUp += mouseUp;
				Controls.Add(b);
			}
			AutoScaleDimensions = new SizeF(17F, 41F);
			AutoScaleMode = AutoScaleMode.Font;
			AutoSize = true;
			ClientSize = new Size(800, 450);
			Name = "MainForm";
			Text = "GoQuest 2030 Room Buttons";
			ResumeLayout(false);
			json = new GoQuestJsonSender();
			int j = 0;
			Thread.Sleep(2000);
			while (true)
			{
				json.GameStart(gameNames[j]);
				json.SuperQuest(gameNames[j]);
				json.GameName(gameNames[j++]);
				//json.Release(); json.Release(); json.Release();
				if (j == gameNames.Count) j = 0;
				Thread.Sleep(100);
			}
		}
		private void play_MouseDown(object sender, MouseEventArgs e) { json.GameStart(((Control)sender).Name); }
		private void super_MouseDown(object sender, MouseEventArgs e) { json.SuperQuest(((Control)sender).Name); }
		private void name_MouseDown(object sender, MouseEventArgs e) { json.GameName(((Control)sender).Name); }
		private void mouseUp(object sender, MouseEventArgs e) { json.Release(); }
	}
}