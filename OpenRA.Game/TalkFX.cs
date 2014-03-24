using System;
using DBus;

[Interface("org.roccat")]
interface IFx 
{
	void SetLedRgb(UInt32 effect, UInt32 ambient_color, UInt32 event_color);
	void RestoreLedRgb();
}

namespace OpenRA
{

	public class TalkFX
	{
		static IFx FX;
		static UInt32 ColourA;
		static UInt32 ColourB;
		const string Bus = "org.roccat";
		const string Service = "/org/roccat";

		public static void TalkFXInit()
		{
			if (Platform.CurrentPlatform == PlatformType.Linux)
			{
				try
				{
					FX = DBus.Bus.Session.GetObject<IFx>(Bus, new ObjectPath(Service));
					try
					{
						TFXModInit();
					}
					catch
					{
						ColourA = 255;
						ColourB = 255;
					}
				}
				catch { }
			}
		}

		public static void TFXGameInit()
		{
			//should check for a colour ramp specified in the map.yaml then fallback to player setting
			var pcolour = Game.Settings.Player.Color.RGB;
			var pcolourh = Game.Settings.Player.Color.H;
			var pcolours = Game.Settings.Player.Color.S;
			var pcolourl = (Math.Min(Math.Max(Game.Settings.Player.Color.L * 1, 0), 140));
			var adjustedrgb = FileFormats.HSLColor.RGBFromHSL(pcolourh / 255f, pcolours / 255f, pcolourl / 255f);
			int rgbint = (adjustedrgb.R << 16 | adjustedrgb.G << 8 | adjustedrgb.B);
			Console.WriteLine("[DEBUG] TalkFX RGB Colour: " + rgbint);
			ColourA = Convert.ToUInt32(rgbint);
			ColourB = Convert.ToUInt32(rgbint);
		}

		public static void TFXModInit()
		{
			//to be coded
		}

		public static void NormalFX()
		{
			FX.SetLedRgb(256, ColourA, ColourB);
		}

		public static void LowPowerFX()
		{
			FX.SetLedRgb(1000, ColourA, ColourB);
		}

		public static void FXOff()
		{
			FX.RestoreLedRgb();
		}
	}
}
