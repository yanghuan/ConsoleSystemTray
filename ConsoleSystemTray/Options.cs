namespace ConsoleSystemTray
{
  internal class Options
  {
    public Options()
    {
    }

    public string Path { get; set; }
    public string Arguments { get; set; }
    public string BaseDirectory { get; set; }
    public string Icon { get; set; }
    public string Tip { get; set; }
    public bool IsPreventSleep { get; set; }
    public bool IsStartMinimized { get; set; }
  }
}