using HarmonyLib;
using Nickel;

namespace Shockah.Johnson;

internal static class TemporaryUpgradesExt
{
	public static bool IsTemporarilyUpgraded(this Card self)
		=> ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(self, "IsTemporarilyUpgraded");

	public static void SetTemporarilyUpgraded(this Card self, bool value)
		=> ModEntry.Instance.Helper.ModData.SetModData(self, "IsTemporarilyUpgraded", value);
}

internal sealed class TemporaryUpgradeManager
{
	internal static ICardTraitEntry Trait = null!;

	public TemporaryUpgradeManager()
	{
		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("TemporaryUpgrade", new()
		{
			Icon = (_, _) => ModEntry.Instance.TemporaryUpgradeIcon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["cardTrait", "TemporaryUpgrade", "name"]).Localize,
			Tooltips = (_, _) => [ModEntry.Instance.Api.TemporaryUpgradeTooltip]
		});

		ModEntry.Instance.Helper.Content.Cards.OnGetDynamicInnateCardTraitOverrides += (_, e) =>
		{
			if (e.Card.upgrade != Upgrade.None && e.Card.IsTemporarilyUpgraded())
				e.SetOverride(Trait, true);
		};

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(State), nameof(State.EndRun)),
			prefix: new HarmonyMethod(GetType(), nameof(State_EndRun_Prefix))
		);

		ModEntry.Instance.Helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnCombatEnd), (State state) =>
		{
			state.rewardsQueue.Queue(new ARemoveTemporaryUpgrades());
		}, 0);
	}

	private static void RemoveTemporaryUpgrades(State state)
	{
		foreach (var card in state.GetAllCards())
		{
			if (!card.IsTemporarilyUpgraded())
				continue;
			card.SetTemporarilyUpgraded(false);
			card.upgrade = Upgrade.None;
		}
	}

	private static void State_EndRun_Prefix(State __instance)
		=> RemoveTemporaryUpgrades(__instance);

	public sealed class ARemoveTemporaryUpgrades : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			RemoveTemporaryUpgrades(s);
		}
	}
}
