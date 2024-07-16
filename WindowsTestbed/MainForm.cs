namespace Lucid.GoQuest
{
	public partial class MainForm : Form
	{
		public MainForm()
		{
			InitializeComponent();
			GoQuest2030.print();
			GoQuest2030.Sequencer.Start();
		}
	}
}