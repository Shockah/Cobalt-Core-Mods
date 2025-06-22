﻿using Nanoray.PluginManager;
using Nickel;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class PlaceholderSecretCard : SecretCard, IDraculaCard
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Secret.Placeholder", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.SpellDeck.Deck,
				rarity = Rarity.common,
				dontOffer = true,
				unreleased = true
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Secret", "Placeholder", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.description = ModEntry.Instance.Localizations.Localize(["card", "Secret", "Placeholder", "description"]);
		return data;
	}
}
