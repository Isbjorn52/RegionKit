using System.Runtime.CompilerServices;
using EffExt;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace RegionKit.Modules.Misc;

internal static class ColoredCeilingDrips
{
	public static void Enable()
	{
        On.WaterDrip.DrawSprites += WaterDrip_DrawSprites;
		IL.Room.Update += IL_Room_Update;
	}

	public static void Disable()
	{
		On.WaterDrip.DrawSprites -= WaterDrip_DrawSprites;
		IL.Room.Update -= IL_Room_Update;
	}

	public static void Setup()
	{
		EffectDefinitionBuilder builder = new EffectDefinitionBuilder("Ceiling Drips Color");

		builder
			.AddFloatField("v", 0f, 1f, 0.01f, 1f, "Brightness")
			.AddFloatField("s", 0f, 1f, 0.01f, 0f, "Saturation")
			.AddFloatField("h", 0f, 1f, 0.01f, 0f, "Hue")
			.SetEffectInitializer(CeilingDripsColorEffectInitializer)
			.SetCategory("RegionKit") 
			.Register(); 
	}

	private static void CeilingDripsColorEffectInitializer(Room room, EffectExtraData data, bool firstTimeRealized)
	{
		room.GetData().ceilingDripsColor = Color.HSVToRGB(data.GetFloat("h"), data.GetFloat("s"), data.GetFloat("v"));
	}

	private static void WaterDrip_DrawSprites(On.WaterDrip.orig_DrawSprites orig, WaterDrip self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		if (self.GetData().isCeilingDrip)
		{
			if (self.room == null) return;

			Color? color = self.room.GetData().ceilingDripsColor;

			if (color != null)
			{
				self.colors = new Color[]
				{
					rCam.currentPalette.blackColor,
					Color.Lerp(rCam.currentPalette.blackColor, color.Value, 0.5f),
					color.Value
				};
			}
		}

		orig(self, sLeaser, rCam, timeStacker, camPos);
	}

	private static void IL_Room_Update(ILContext il)
	{
		ILCursor cursor = new ILCursor(il);

		cursor.GotoNext(MoveType.After,
			x => x.MatchNewobj<WaterDrip>(),
			x => x.MatchCall<Room>(nameof(Room.AddObject))
			);

		cursor.Emit(OpCodes.Ldarg_0);
		cursor.EmitDelegate<Action<Room>>((self) =>
		{
			(self.updateList[self.updateList.Count - 1] as WaterDrip).GetData().isCeilingDrip = true;
		});
	}

	#region Extentions

	private static readonly ConditionalWeakTable<WaterDrip, WaterDripData> waterDripCWT = new ConditionalWeakTable<WaterDrip, WaterDripData>();

	public class WaterDripData
	{
		public bool isCeilingDrip = false;

		public WaterDripData() { }
	}

	public static WaterDripData GetData(this WaterDrip waterDrip)
	{
		if (!waterDripCWT.TryGetValue(waterDrip, out WaterDripData waterDripData))
		{
			waterDripData = new WaterDripData();
			waterDripCWT.Add(waterDrip, waterDripData);
		}

		return waterDripData;
	}

	private static ConditionalWeakTable<Room, RoomData> roomCWT = new ConditionalWeakTable<Room, RoomData>();

	public class RoomData
	{
		public Color? ceilingDripsColor;

		public RoomData() { }
	}

	public static RoomData GetData(this Room waterDrip)
	{
		if (!roomCWT.TryGetValue(waterDrip, out RoomData waterDripData))
		{
			waterDripData = new RoomData();
			roomCWT.Add(waterDrip, waterDripData);
		}

		return waterDripData;
	}

	#endregion
}
