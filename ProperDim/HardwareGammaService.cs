using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ProperDim;

public class HardwareGammaService : IDisposable
{
	private Timer _watchdogTimer;
	private bool _isRunning;
	private readonly int _intervalMs = 1000;

	private readonly ConcurrentDictionary<string, double> _targetGammas = new();
	private readonly ConcurrentDictionary<string, double> _appliedGammas = new();
	private readonly Lock _rampLock = new();
	private double _currentMagFactor = 1.0;

	private RAMP _sharedRamp = new()
	{
		Red = new ushort[256],
		Green = new ushort[256],
		Blue = new ushort[256]
	};

	public HardwareGammaService()
	{
		_isRunning = false;
		NativeMethods.MagInitialize();
	}

	public void Start()
	{
		if (_isRunning) return;
		_isRunning = true;
		_watchdogTimer = new Timer(WatchdogTick, null, 0, _intervalMs);
	}

	public void Stop()
	{
		_isRunning = false;
		_watchdogTimer?.Change(Timeout.Infinite, Timeout.Infinite);
	}

	public void SetTargetGamma(string deviceName, double gamma)
	{
		_targetGammas[deviceName] = gamma;
		ApplyGamma(deviceName, gamma);
	}

	public static bool TestGammaSupport(string deviceName)
	{
		IntPtr dc = NativeMethods.CreateDC(null, deviceName, null, IntPtr.Zero);
		if (dc != IntPtr.Zero)
		{
			RAMP testRamp = new() { Red = new ushort[256], Green = new ushort[256], Blue = new ushort[256] };
			// This returns false if the driver lacks a physical Look-Up Table
			bool supported = NativeMethods.GetDeviceGammaRamp(dc, ref testRamp);
			NativeMethods.DeleteDC(dc);
			return supported;
		}
		return false;
	}

	public void SetGlobalMagnification(double globalBrightness)
	{
		double visualFactor = 1.0;

		if (globalBrightness < 0.5)
		{
			// Prevent negative numbers from reaching Math.Pow, which causes a NaN crash
			double safeBrightness = Math.Max(0.0, globalBrightness);

			// Normalize the full 0.00 to 0.50 range to a 0.0 to 1.0 scale
			double normalized = safeBrightness / 0.5;

			// Apply an easing exponent to accelerate the initial drop from 50%
			// and slow down the changes near the bottom. 
			double eased = Math.Pow(normalized, 1.5);

			// Map the eased value to the safe linear floor (0.4 to 1.0)
			double linearFactor = 0.05 + (eased * 0.95);

			// Apply the 2.2 gamma curve to map the linear math to human perceptual brightness
			visualFactor = Math.Pow(linearFactor, 2.2);
		}

		if (Math.Abs(_currentMagFactor - visualFactor) < 0.001) return;
		_currentMagFactor = visualFactor;

		float f = (float)visualFactor;
		var effect = new MAGCOLOREFFECT
		{
			transform = [
				f, 0, 0, 0, 0,
				0, f, 0, 0, 0,
				0, 0, f, 0, 0,
				0, 0, 0, 1, 0,
				0, 0, 0, 0, 1
			]
		};
		NativeMethods.MagSetFullscreenColorEffect(ref effect);
	}

	private void WatchdogTick(object state)
	{
		if (!_isRunning) return;
		foreach (var kvp in _targetGammas)
		{
			ApplyGamma(kvp.Key, kvp.Value, force: true);
		}
	}

	private void ApplyGamma(string deviceName, double targetGamma, bool force = false)
	{
		// Hard floor at 0.5 to respect Windows Driver limits and prevent screen blackouts.
		double hardwareTarget = Math.Max(0.5, Math.Min(1.0, targetGamma));

		if (!force && _appliedGammas.TryGetValue(deviceName, out double lastApplied) && Math.Abs(hardwareTarget - lastApplied) < 0.001)
		{
			return;
		}

		lock (_rampLock)
		{
			for (int i = 0; i < 256; i++)
			{
				double normalized = i / 255.0;

				// Linear scaling: Proportional reduction of the white point ceiling instead of crushing midtones
				ushort value = (ushort)(normalized * hardwareTarget * 65535);

				_sharedRamp.Red[i] = value;
				_sharedRamp.Green[i] = value;
				_sharedRamp.Blue[i] = value;
			}

			IntPtr dc = IntPtr.Zero;
			try
			{
				dc = NativeMethods.CreateDC(null, deviceName, null, IntPtr.Zero);
				if (dc != IntPtr.Zero)
				{
					if (NativeMethods.SetDeviceGammaRamp(dc, ref _sharedRamp))
					{
						_appliedGammas[deviceName] = hardwareTarget;
					}
				}
			}
			finally
			{
				if (dc != IntPtr.Zero)
				{
					NativeMethods.DeleteDC(dc);
				}
			}
		}
	}

	public void Dispose()
	{
		Stop();
		_watchdogTimer?.Dispose();
		NativeMethods.MagUninitialize();
		GC.SuppressFinalize(this);
	}
}