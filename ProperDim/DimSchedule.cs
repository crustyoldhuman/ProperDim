using System;

namespace ProperDim;

// This is the ONLY place this class should be defined.
public class DimSchedule
{
	public TimeSpan Time { get; set; }
	public double Brightness { get; set; }
	public string Days { get; set; }

	// Helper properties for display in lists
	// Helper properties for display in lists
	public string DisplayTime
	{
		get
		{
			bool use24h = ConfigManager.Settings.UseMilitaryTime;
			return DateTime.Today.Add(Time).ToString(use24h ? "HH:mm" : "h:mm tt");
		}
	}

	// Renamed from DisplayBrightness to Display to match your ControlPanel.xaml binding
	public string Display => $"{Math.Round(Brightness * 100)}%";
}