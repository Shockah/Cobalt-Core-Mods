using Microsoft.Xna.Framework;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class BatFormCard : Card, IDraculaCard
{
	[JsonProperty]
	public int FlipIndex { get; private set; } = 0;

	[JsonProperty]
	private bool LastFlipped { get; set; }

	public float ActionSpacingScaling
		=> 1.5f;

	public Matrix ModifyNonTextCardRenderMatrix(G g, List<CardAction> actions)
	{
		if (upgrade == Upgrade.B)
			return Matrix.CreateScale(1.5f);
		else
			return Matrix.Identity;
	}

	public Matrix ModifyCardActionRenderMatrix(G g, List<CardAction> actions, CardAction action, int actionWidth)
	{
		if (upgrade == Upgrade.B)
			return Matrix.CreateScale(1f / 1.5f);

		var spacing = 48;
		var newXOffset = 48;
		var newYOffset = 40;
		var index = actions.IndexOf(action);
		return index switch
		{
			0 => Matrix.CreateTranslation(-newXOffset, -newYOffset - (int)((index - actions.Count / 2.0 + 0.5) * spacing), 0),
			1 => Matrix.CreateTranslation(newXOffset, -newYOffset - (int)((index - actions.Count / 2.0 + 0.5) * spacing), 0),
			2 => Matrix.CreateTranslation(newXOffset, newYOffset - (int)((index - actions.Count / 2.0 + 0.5) * spacing), 0),
			3 => Matrix.CreateTranslation(-newXOffset, newYOffset - (int)((index - actions.Count / 2.0 + 0.5) * spacing), 0),
			_ => Matrix.Identity
		};
	}

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("BatForm", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "BatForm", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.A ? 0 : 1,
			floppable = true
		};

	public override void ExtraRender(G g, Vec v)
	{
		base.ExtraRender(g, v);
		if (LastFlipped != flipped)
		{
			LastFlipped = flipped;
			FlipIndex = (FlipIndex + 1) % (upgrade == Upgrade.B ? 3 : 4);
		}
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AMove
				{
					targetPlayer = true,
					dir = 1,
					isRandom = true,
				}.Disabled(FlipIndex % 3 != 0),
				new AMove
				{
					targetPlayer = true,
					dir = 2,
					isRandom = true
				}.Disabled(FlipIndex % 3 != 1),
				new AMove
				{
					targetPlayer = true,
					dir = 3,
					isRandom = true
				}.Disabled(FlipIndex % 3 != 2)
			],
			_ => [
				new AMove
				{
					targetPlayer = true,
					dir = -1,
					ignoreFlipped = true
				}.Disabled(FlipIndex % 4 != 0),
				new AMove
				{
					targetPlayer = true,
					dir = 1,
					ignoreFlipped = true
				}.Disabled(FlipIndex % 4 != 1),
				new AMove
				{
					targetPlayer = true,
					dir = 2,
					ignoreFlipped = true
				}.Disabled(FlipIndex % 4 != 2),
				new AMove
				{
					targetPlayer = true,
					dir = -2,
					ignoreFlipped = true
				}.Disabled(FlipIndex % 4 != 3)
			]
		};
}
