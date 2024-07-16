using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;

namespace Shockah.Natasha;

internal static class TimesPlayedExt
{
	public static void ResetTimesPlayed(this Card card)
		=> ModEntry.Instance.Helper.ModData.RemoveModData(card, "TimesPlayed");

	public static int GetTimesPlayed(this Card card)
		=> ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(card, "TimesPlayed");

	public static void SetTimesPlayed(this Card card, int value)
		=> ModEntry.Instance.Helper.ModData.SetModData(card, "TimesPlayed", Math.Max(value, 0));
}

internal sealed class TimesPlayed : IRegisterable
{
	internal static ISpriteEntry Icon { get; private set; } = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		Icon = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Icons/TimesPlayed.png"));

		helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnCombatEnd), (State state) =>
		{
			foreach (var card in state.deck)
				card.ResetTimesPlayed();
		}, 0);

		helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnPlayerPlayCard), (Card card, State state) =>
		{
			card.SetTimesPlayed(card.GetTimesPlayed() + 1);
		}, 0);
	}
}

internal sealed class TimesPlayedVariableHint : AVariableHint
{
	public required int CardId;

	public TimesPlayedVariableHint()
	{
		this.hand = true;
	}

	public override Icon? GetIcon(State s)
		=> new() { path = TimesPlayed.Icon.Sprite };

	public override List<Tooltip> GetTooltips(State s)
		=> [
			new GlossaryTooltip("action.xHintTimesPlayed.desc")
			{
				Description = ModEntry.Instance.Localizations.Localize(["x", "TimesPlayed", s.route is Combat ? "stateful" : "stateless"], new { Count = (s.FindCard(CardId)?.GetTimesPlayed() ?? 0) + 1 })
			}
		];
}

internal sealed class TimesPlayedCondition : IKokoroApi.IConditionalActionApi.IIntExpression
{
	public required int CardId;

	public string GetTooltipDescription(State state, Combat? combat)
		=> ModEntry.Instance.Localizations.Localize(["condition", "TimesPlayed"]);

	public int GetValue(State state, Combat combat)
		=> state.FindCard(CardId)?.GetTimesPlayed() ?? 0;

	public void Render(G g, ref Vec position, bool isDisabled, bool dontRender)
	{
		if (!dontRender)
			Draw.Sprite(TimesPlayed.Icon.Sprite, position.x, position.y, color: isDisabled ? Colors.disabledIconTint : Colors.white);
		position.x += 8;
	}
}