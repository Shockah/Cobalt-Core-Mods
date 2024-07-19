﻿using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class SecondConsoleArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("SecondConsole", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.NatashaDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/SecondConsole.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "SecondConsole", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "SecondConsole", "description"]).Localize
		});
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> [
			.. (Limited.Trait.Configuration.Tooltips?.Invoke(DB.fakeState, null) ?? []),
			.. StatusMeta.GetTooltips(Status.tempShield, 2),
		];

	public override void OnPlayerPlayCard(int energyCost, Deck deck, Card card, State state, Combat combat, int handPosition, int handCount)
	{
		if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, Limited.Trait))
			return;

		combat.QueueImmediate(new AStatus
		{
			targetPlayer = true,
			status = Status.tempShield,
			statusAmount = 1,
			artifactPulse = Key(),
		});
	}
}