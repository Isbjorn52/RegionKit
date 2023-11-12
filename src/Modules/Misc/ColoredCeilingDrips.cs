using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace RegionKit.Modules.Misc;

internal static class ColoredCeilingDrips
{
	private static Dictionary<string, Color> regionCeilingDripColors = new Dictionary<string, Color>();

	public static void Enable()
	{
		On.Region.ctor += Region_ctor;
        On.WaterDrip.DrawSprites += WaterDrip_DrawSprites;
		IL.Room.Update += Room_Update;
	}

	public static void Disable()
	{
		On.Region.ctor -= Region_ctor;
		On.WaterDrip.DrawSprites -= WaterDrip_DrawSprites;
		IL.Room.Update -= Room_Update;
	}

	private static void Region_ctor(On.Region.orig_ctor orig, Region self, string name, int firstRoomIndex, int regionNumber, SlugcatStats.Name storyIndex)
	{
		orig(self, name, firstRoomIndex, regionNumber, storyIndex);

		string text = "";

		if (storyIndex != null)
		{
			text = "-" + storyIndex.value;
		}
		string path = AssetManager.ResolveFilePath(string.Concat(new string[]
		{
			"World",
			Path.DirectorySeparatorChar.ToString(),
			name,
			Path.DirectorySeparatorChar.ToString(),
			"properties",
			text,
			".txt"
		}));
		if (text != "" && !File.Exists(path))
		{
			path = AssetManager.ResolveFilePath(string.Concat(new string[]
			{
				"World",
				Path.DirectorySeparatorChar.ToString(),
				name,
				Path.DirectorySeparatorChar.ToString(),
				"properties.txt"
			}));
		}

		if (File.Exists(path))
		{

			Debug.Log("File exists at " + path);
			string[] array = File.ReadAllLines(path);

			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = array[i].Split(new string[] { ":", ": " }, StringSplitOptions.None);

				if (array2[0].ToLower() == "ceilingdripscolor")
				{
					string[] array3 = array2[1].Split(',');

					Color color = new Color(
						float.Parse(array3[0]),
						float.Parse(array3[1]),
						float.Parse(array3[2])
						);

					Debug.Log("Color found");
					Debug.Log(color.r);
					Debug.Log(color.g);
					Debug.Log(color.b);

					//self.regionParams.GetData().ceilingDripsColor = color;
					//self.regionParams.GetData().ceilingDripsColor = color;
					if (!regionCeilingDripColors.ContainsKey(self.name))
					{
						regionCeilingDripColors.Add(self.name, color);
						Debug.Log("new key!");

					}
					else
					{
						Debug.Log("not new key!");

						regionCeilingDripColors[self.name] = color;
					}
				}
			}
		}
	}

	private static void WaterDrip_DrawSprites(On.WaterDrip.orig_DrawSprites orig, WaterDrip self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		if (self.GetData().isCeilingDrip)
		{
			if (self.room == null) return;

			if (regionCeilingDripColors.TryGetValue(self.room.world.region.name, out Color color))
			{
				self.colors = new Color[]
				{
					rCam.currentPalette.blackColor,
					Color.Lerp(rCam.currentPalette.blackColor, color, 0.5f),
					color
				};
			}
		}

		orig(self, sLeaser, rCam, timeStacker, camPos);
	}

	private static void Room_Update(ILContext il)
	{
		ILCursor cursor = new ILCursor(il);

		cursor.GotoNext(
			x => x.MatchNewobj<WaterDrip>()
			);

		cursor.Index += 2;

		cursor.Emit(OpCodes.Ldarg_0);

		cursor.EmitDelegate<Action<Room>>((self) =>
		{
			(self.updateList[self.updateList.Count - 1] as WaterDrip).GetData().isCeilingDrip = true;
		});
	}


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
}
