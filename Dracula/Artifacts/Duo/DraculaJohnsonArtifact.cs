using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class DraculaJohnsonArtifact : Artifact, IRegisterable
{
	private static IJohnsonApi JohnsonApi = null!;
	private static ISpriteEntry TemporaryToPermanentUpgradeIcon = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;
		if (ModEntry.Instance.JohnsonApi is not { } johnsonApi)
			return;
		JohnsonApi = johnsonApi;

		TemporaryToPermanentUpgradeIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/TemporaryToPermanentUpgrade.png"));

		helper.Content.Artifacts.RegisterArtifact("DraculaJohnson", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = [ArtifactPool.Common]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/DraculaJohnson.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DraculaJohnson", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DraculaJohnson", "description"]).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.DraculaDeck.Deck, johnsonApi.JohnsonDeck.Deck]);

		ModEntry.Instance.BloodTapManager.RegisterOptionProvider(new Provider(), -1000);
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> [
			new TTCard { card = new BloodTapCard { discount = -1, temporaryOverride = true, exhaustOverride = true } },
			ModEntry.Instance.JohnsonApi!.TemporaryUpgradeTooltip
		];

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		if (!combat.isPlayerTurn || combat.turn != 1)
			return;

		combat.Queue(new AAddCard
		{
			destination = CardDestination.Hand,
			card = new BloodTapCard { discount = -1, temporaryOverride = true, exhaustOverride = true },
			artifactPulse = Key()
		});
	}

	private sealed class Provider : IBloodTapOptionProvider
	{
		public IEnumerable<Status> GetBloodTapApplicableStatuses(State state, Combat combat, IReadOnlySet<Status> allStatuses)
			=> [];

		public IEnumerable<List<CardAction>> GetBloodTapOptionsActions(State state, Combat combat, IReadOnlySet<Status> allStatuses)
		{
			if (state.EnumerateAllArtifacts().FirstOrDefault(a => a is DraculaJohnsonArtifact) is not { } artifact)
				yield break;

			artifact.Pulse();
			yield return [
				new AHurt { targetPlayer = true, hurtAmount = 4 },
				new Action()
			];
		}
	}

	private sealed class Action : DynamicWidthCardAction
	{
		public override Icon? GetIcon(State s)
			=> new(TemporaryToPermanentUpgradeIcon.Sprite, null, Colors.textMain);

		public override List<Tooltip> GetTooltips(State s)
			=> [
				new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::DraculaJohnsonDuo::Action")
				{
					Icon = TemporaryToPermanentUpgradeIcon.Sprite,
					TitleColor = Colors.action,
					Title = $"{Tooltip.GetIndent()}{ModEntry.Instance.Localizations.Localize(["artifact", "Duo", "DraculaJohnson", "action", "name"])}",
					Description = ModEntry.Instance.Localizations.Localize(["artifact", "Duo", "DraculaJohnson", "action", "description"])
				}
			];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			c.QueueImmediate(ModEntry.Instance.KokoroApi.Actions.MakeCustomCardBrowse(new ACardSelect
			{
				browseAction = new BrowseAction()
			}, new TempUpgradeBrowseSource()));
		}
	}

	private sealed class BrowseAction : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			if (selectedCard is null)
				return;
			if (ModEntry.Instance.JohnsonApi is not { } johnsonApi)
				return;

			johnsonApi.SetTemporarilyUpgraded(selectedCard, false);
		}
	}

	public sealed class TempUpgradeBrowseSource : ICustomCardBrowseSource
	{
		public string GetTitle(State state, Combat? combat, List<Card> cards)
			=> ModEntry.Instance.Localizations.Localize(["artifact", "Duo", "DraculaJohnson", "browseTitle"], new { Count = cards.Count });

		public List<Card> GetCards(State state, Combat? combat)
			=> state.deck.Concat(combat?.hand ?? []).Concat(combat?.discard ?? []).Where(JohnsonApi.IsTemporarilyUpgraded).ToList();
	}
}