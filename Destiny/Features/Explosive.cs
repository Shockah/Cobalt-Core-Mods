using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Destiny;

internal sealed class Explosive : IRegisterable
{
	internal static ICardTraitEntry ExplosiveTrait { get; private set; } = null!;

	private static readonly Pool<ModifyExplosiveDamageArgs> ModifyExplosiveDamageArgsPool = new(() => new());

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Traits/Explosive.png"));
		
		ExplosiveTrait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("Explosive", new()
		{
			Icon = (_, _) => icon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["cardTrait", "Explosive", "name"]).Localize,
			Tooltips = (state, card) => [
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::Explosive")
				{
					Icon = icon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["cardTrait", "Explosive", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(
						["cardTrait", "Explosive", "description"], 
						new { Damage = Card.GetActualDamage(state, GetExplosiveDamage(state, state.route as Combat ?? DB.fakeCombat, card), card: card) }
					),
				}
			]
		});
		
		ModEntry.Instance.Helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnPlayerPlayCard), (Card card, State state, Combat combat) =>
		{
			if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, ExplosiveTrait))
				return;
			combat.Queue(new AAttack { damage = card.GetDmg(state, GetExplosiveDamage(state, combat, card)) });
			ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(state, card, ExplosiveTrait, false, permanent: false);
		});
	}

	private static int GetExplosiveDamage(State state, Combat combat, Card? card)
		=> ModifyExplosiveDamageArgsPool.Do(args =>
		{
			args.State = state;
			args.Combat = combat;
			args.Card = card;
			args.BaseDamage = 4;
			args.CurrentDamage = args.BaseDamage;
				
			foreach (var hook in ModEntry.Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
				hook.ModifyExplosiveDamage(args);
			return args.CurrentDamage;
		});

	private sealed class ModifyExplosiveDamageArgs : IDestinyApi.IHook.IModifyExplosiveDamageArgs
	{
		public State State { get; set; } = null!;
		public Combat Combat { get; set; } = null!;
		public Card? Card { get; set; }
		public int BaseDamage { get; set; }
		public int CurrentDamage { get; set; }
	}
}