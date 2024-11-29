using Nickel;
using System.Linq;

namespace Shockah.Johnson;

internal static class StrengthenExt
{
	public static int GetStrengthen(this Card self)
		=> ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(self, "Strengthen");

	public static void SetStrengthen(this Card self, int value)
		=> ModEntry.Instance.Helper.ModData.SetModData(self, "Strengthen", value);

	public static void AddStrengthen(this Card self, int value)
	{
		if (value != 0)
			self.SetStrengthen(self.GetStrengthen() + value);
	}
}

internal sealed class StrengthenManager
{
	internal static ICardTraitEntry Trait = null!;

	public StrengthenManager()
	{
		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("Strengthen", new()
		{
			Icon = (_, _) => ModEntry.Instance.StrengthenIcon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["cardTrait", "Strengthen", "name"]).Localize,
			Tooltips = (_, card) => [ModEntry.Instance.Api.GetStrengthenTooltip(card?.GetStrengthen() ?? 1)]
		});

		ModEntry.Instance.Helper.Content.Cards.OnGetDynamicInnateCardTraitOverrides += (_, e) =>
		{
			if (e.Card.GetStrengthen() != 0)
				e.SetOverride(Trait, true);
		};

		ModEntry.Instance.Helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.ModifyBaseDamage), (Card? card, State state) =>
		{
			if (card is null)
				return 0;

			var strengthen = card.GetStrengthen();
			if (strengthen > 0 && state.EnumerateAllArtifacts().Any(a => a is JohnsonPeriArtifact))
				strengthen++;
			return strengthen;
		});

		ModEntry.Instance.Helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnCombatEnd), (State state) =>
		{
			foreach (var card in state.deck)
			{
				if (card.GetStrengthen() == 0)
					continue;
				card.SetStrengthen(0);
			}
		});
	}
}
