using System.Runtime.InteropServices;

namespace Lucid.GoQuest
{
	static class Program
	{
		[DllImport("kernel32.dll")]
		static extern IntPtr GetConsoleWindow();
		[DllImport("user32.dll")]
		static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
		const int SW_HIDE = 0;
		const int SW_SHOW = 5;
		[STAThread]
		static void Main()
		{
			ConsoleWindow(true);
			StdOut.WriteStr = Console.WriteLine;
			StdOut.WriteFmt = Console.WriteLine;
			GoQuest.Path = Directory.GetCurrentDirectory() + @"\..\..\..\";
			GoQuest.Print();
			AutoPlayer.Start();
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainForm());
		}
		public static void ConsoleWindow(bool b)
		{
			var handle = GetConsoleWindow();
			ShowWindow(handle, b ? SW_SHOW : SW_HIDE);
		}
	}
}