using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;

namespace Shockah.Destiny;

internal sealed class ImbueManager : IRegisterable
{
	internal static ISpriteEntry Icon { get; private set; } = null!;

	private static readonly Color OutlineColor = new("51A7F8");

	private static ISpriteEntry? DiscountOutlineSprite;
	private static readonly Dictionary<string, ISpriteEntry> TraitOutlineSprites = [];
	
	private static Card? CardRendered;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		Icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Actions/Imbue.png"));
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.MakeAllActionIcons)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_MakeAllActionIcons_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_MakeAllActionIcons_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Prefix))
		);
	}

	private static void Card_MakeAllActionIcons_Prefix(Card __instance)
		=> CardRendered = __instance;

	private static void Card_MakeAllActionIcons_Finalizer()
		=> CardRendered = null;

	private static bool Card_RenderAction_Prefix(G g, State state, CardAction action, bool dontDraw, ref int __result)
	{
		if (action is not IImbueAction anyImbueAction)
			return true;
		
		var renderAsDisabled = state != DB.fakeState && (action.disabled || (CardRendered is not null && EnchantedManager.GetEnchantLevel(CardRendered) != anyImbueAction.Level - 1));
		
		if (action is ImbueAction imbueAction)
		{
			var position = g.Push(rect: new()).rect.xy;
			var initialX = (int)position.x;

			if (!dontDraw)
				Draw.Sprite(Icon.Sprite, position.x, position.y, color: renderAsDisabled ? Colors.disabledIconTint : Colors.white);
			position.x += 14;

			if (imbueAction.Trait?.Configuration.Icon?.Invoke(state, null) is { } traitIcon)
			{
				position.x += 2;

				if (!TraitOutlineSprites.TryGetValue(imbueAction.Trait.UniqueName, out var traitOutlineSprite))
				{
					traitOutlineSprite = TextureOutlines.CreateOutlineSprite(traitIcon, true, true, false);
					TraitOutlineSprites[imbueAction.Trait.UniqueName] = traitOutlineSprite;
				}

				if (!dontDraw)
				{
					Draw.Sprite(traitOutlineSprite.Sprite, position.x, position.y - 1, color: renderAsDisabled ? Colors.disabledIconTint * OutlineColor : OutlineColor);
					Draw.Sprite(traitIcon, position.x + 1, position.y, color: renderAsDisabled ? Colors.disabledIconTint : Colors.white);
				}
				position.x += 11;
			}

			__result = (int)position.x - initialX;
			g.Pop();

			return false;
		}
		
		if (action is ImbueDiscountAction)
		{
			var position = g.Push(rect: new()).rect.xy;
			var initialX = (int)position.x;

			if (!dontDraw)
				Draw.Sprite(Icon.Sprite, position.x, position.y, color: renderAsDisabled ? Colors.disabledIconTint : Colors.white);
			position.x += 14;

			position.x += 2;

			DiscountOutlineSprite ??= TextureOutlines.CreateOutlineSprite(StableSpr.icons_discount, true, true, false);
			
			if (!dontDraw)
			{
				Draw.Sprite(DiscountOutlineSprite.Sprite, position.x, position.y - 1, color: renderAsDisabled ? Colors.disabledIconTint * OutlineColor : OutlineColor);
				Draw.Sprite(StableSpr.icons_discount, position.x + 1, position.y, color: renderAsDisabled ? Colors.disabledIconTint : Colors.white);
			}
			position.x += 11;

			__result = (int)position.x - initialX;
			g.Pop();

			return false;
		}

		return true;
	}
}

internal interface IImbueAction
{
	int Level { get; }
	
	void ImbueCard(State state, Card card);
}

internal sealed class ImbueAction : CardAction, IImbueAction
{
	public required int Level { get; set; }
	
	[JsonIgnore]
	public required ICardTraitEntry? Trait
	{
		get => TraitUniqueName is null ? null : ModEntry.Instance.Helper.Content.Cards.LookupTraitByUniqueName(TraitUniqueName);
		set => TraitUniqueName = value?.UniqueName;
	}

	[JsonProperty]
	private string? TraitUniqueName;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;
	}

	public void ImbueCard(State state, Card card)
	{
		if (Trait is not { } trait)
			return;
		ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(state, card, trait, true, permanent: false);
	}
}

internal sealed class ImbueDiscountAction : CardAction, IImbueAction
{
	public required int Level { get; set; }
	
	public int Discount = -1;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;
	}

	public void ImbueCard(State state, Card card)
		=> card.discount += Discount;
}