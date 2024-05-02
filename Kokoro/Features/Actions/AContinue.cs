using System;
using System.Collections.Generic;

namespace Shockah.Kokoro;

public sealed class AContinue : CardAction
{
	private static ModEntry Instance => ModEntry.Instance;

	public Guid Id;
	public bool Continue;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;

		var continueFlags = Instance.Api.ObtainExtensionData(c, Continue ? "ContinueFlags" : "StopFlags", () => new HashSet<Guid>());
		continueFlags.Add(Id);
	}

	public override List<Tooltip> GetTooltips(State s)
		=> [
			new CustomTTGlossary(
				CustomTTGlossary.GlossaryType.action,
				() => (Spr)(Continue ? ModEntry.Instance.Content.ContinueSprite : ModEntry.Instance.Content.StopSprite).Id!.Value,
				() => Continue ? I18n.ContinueActionName : I18n.StopActionName,
				() => Continue ? I18n.ContinueActionDescription : I18n.StopActionDescription,
				key: $"AContinue.{(Continue ? "Continue" : "Stop")}"
			)
		];

	public override Icon? GetIcon(State s)
		=> new(
			path: (Spr)(Continue ? ModEntry.Instance.Content.ContinueSprite : ModEntry.Instance.Content.StopSprite).Id!.Value,
			number: null,
			color: Colors.white
		);
}