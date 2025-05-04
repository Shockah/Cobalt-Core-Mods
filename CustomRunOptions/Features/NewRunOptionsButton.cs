using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.CustomRunOptions;

internal sealed class NewRunOptionsButton : IRegisterable
{
	private static readonly UK CustomOptionsUk = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
	
	private static ISpriteEntry BackgroundOverlaySprite = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		BackgroundOverlaySprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/NewRunOptionsOverlaySprite.png"));
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.RenderWarning)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(NewRunOptions_RenderWarning_Prefix_First)), priority: Priority.First)
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.DifficultyOptions)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(NewRunOptions_DifficultyOptions_Prefix))
		);
	}

	private static void NewRunOptions_RenderWarning_Prefix_First()
		=> Draw.Sprite(BackgroundOverlaySprite.Sprite, 0, 0);

	private static void NewRunOptions_DifficultyOptions_Prefix(G g, RunConfig runConfig)
	{
		var elements = ModEntry.CustomRunOptions.SelectMany(o => o.GetNewRunOptionsElements(g, runConfig)).ToList();
		var buttonResult = SharedArt.ButtonText(g, new Vec(301, 70), CustomOptionsUk, elements.Count == 0 ? "CUSTOM" : "", onMouseDown: new MouseDownHandler(() => { }));

		var totalWidth = 0;
		var elementsFitting = 0;
		for (var i = 0; i < elements.Count; i++)
		{
			var newTotalWidth = totalWidth;
			if (i != 0)
				newTotalWidth += 1;
			newTotalWidth += (int)elements[i].Size.x;

			if (newTotalWidth < 57)
			{
				totalWidth = newTotalWidth;
				elementsFitting++;
				continue;
			}

			var plusWidth = (int)Draw.Text("+", 0, 0, dontDraw: true).w + 2 + $"{elementsFitting - i}".Length * 6;
			newTotalWidth = totalWidth + plusWidth;

			if (newTotalWidth < 57)
			{
				totalWidth = newTotalWidth;
				break;
			}

			if (i > 0)
			{
				totalWidth -= 1;
				totalWidth -= (int)elements[i - 1].Size.x;
				elementsFitting--;
				
				plusWidth = (int)Draw.Text("+", 0, 0, dontDraw: true).w + $"{elementsFitting - i}".Length * 6;
				newTotalWidth = totalWidth + plusWidth - 2;
				totalWidth = newTotalWidth;
			}
		}

		var yPosition = buttonResult.v.y + 25 / 2 + (buttonResult.isHover ? 1 : 0) - 1;
		var xPosition = (int)(buttonResult.v.x + 61 / 2 - totalWidth / 2);
		
		for (var i = 0; i < elementsFitting; i++)
		{
			elements[i].Render(g, new Vec(xPosition, yPosition - elements[i].Size.y / 2));
			xPosition += 1;
			xPosition += (int)elements[i].Size.x;
		}

		if (elementsFitting < elements.Count)
		{
			var textRect = Draw.Text("+", xPosition, yPosition - 2, color: Colors.textMain, outline: Colors.black);
			xPosition += (int)textRect.w;
			
			BigNumbers.Render(elements.Count - elementsFitting, xPosition, yPosition - 4, Colors.textMain);
		}
	}
}