using HarmonyLib;
using Microsoft.Xna.Framework;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class DominateCard : Card, IDraculaCard
{
	private static ISpriteEntry NonFlipArt = null!;

	private static bool IsDuringTryPlayCard = false;

	public Matrix ModifyCardActionRenderMatrix(G g, List<CardAction> actions, CardAction action, int actionWidth)
	{
		if (upgrade == Upgrade.None)
			return Matrix.Identity;

		var spacing = 12 * g.mg.PIX_SCALE;
		var halfYCenterOffset = 16 * g.mg.PIX_SCALE;
		var index = actions.IndexOf(action);
		var recenterY = -(int)((index - actions.Count / 2.0 + 0.5) * spacing);
		return index switch
		{
			0 => Matrix.CreateTranslation(0, recenterY - halfYCenterOffset, 0),
			1 or 2 => Matrix.CreateTranslation(0, recenterY + halfYCenterOffset - spacing / 2 + spacing * (index - 1), 0),
			_ => Matrix.Identity
		};
	}

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		NonFlipArt = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/DominateNonFlip.png"));

		helper.Content.Cards.RegisterCard("Dominate", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.uncommon,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Dominate.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Dominate", "name"]).Localize
		});

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Finalizer))
		);
	}

	public override CardData GetData(State state)
		=> new()
		{
			art = upgrade != Upgrade.None && flipped ? NonFlipArt.Sprite : null,
			cost = 1,
			floppable = upgrade != Upgrade.None
		};

	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> actions = [];

		if (IsDuringTryPlayCard)
		{
			for (var i = 0; i < s.ship.parts.Count; i++)
				if (s.ship.parts[i].type == PType.missiles)
					actions.Add(new APositionalDroneFlip
					{
						WorldX = s.ship.x + i,
						disabled = upgrade != Upgrade.None && flipped
					});
		}
		else
		{
			actions.Add(new APositionalDroneFlip
			{
				WorldX = s.ship.x,
				disabled = upgrade != Upgrade.None && flipped
			});
		}

		if (upgrade == Upgrade.A)
		{
			if (IsDuringTryPlayCard)
			{
				for (var i = 0; i < s.ship.parts.Count; i++)
					if (s.ship.parts[i].type == PType.missiles)
						actions.Add(new APositionalDroneBubble
						{
							WorldX = s.ship.x + i
						});
			}
			else
			{
				actions.Add(new APositionalDroneBubble
				{
					WorldX = s.ship.x
				});
			}
		}
		else if (upgrade == Upgrade.B)
		{
			if (IsDuringTryPlayCard)
			{
				for (var i = 0; i < s.ship.parts.Count; i++)
					if (s.ship.parts[i].type == PType.missiles)
						actions.Add(new APositionalDroneTrigger
						{
							WorldX = s.ship.x + i
						});
			}
			else
			{
				actions.Add(new APositionalDroneTrigger
				{
					WorldX = s.ship.x
				});
			}
		}

		actions.Add(new ADrawCard
		{
			count = 1
		});

		return actions;
	}

	private static void Combat_TryPlayCard_Prefix()
		=> IsDuringTryPlayCard = true;

	private static void Combat_TryPlayCard_Finalizer()
		=> IsDuringTryPlayCard = false;
}
