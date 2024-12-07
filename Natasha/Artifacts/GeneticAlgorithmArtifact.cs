using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using System.Linq;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class GeneticAlgorithmArtifact : Artifact, IRegisterable, IKokoroApi.IV2.ILimitedApi.IHook
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("GeneticAlgorithm", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.NatashaDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/GeneticAlgorithm.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "GeneticAlgorithm", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "GeneticAlgorithm", "description"]).Localize
		});
	}

	private static int GetExtraLimitedUses(Card card)
		=> ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(card, "ExtraLimitedUses");

	private static void SetExtraLimitedUses(Card card, int value)
		=> ModEntry.Instance.Helper.ModData.SetModData(card, "ExtraLimitedUses", value);

	public override void OnPlayerPlayCard(int energyCost, Deck deck, Card card, State state, Combat combat, int handPosition, int handCount)
	{
		base.OnPlayerPlayCard(energyCost, deck, card, state, combat, handPosition, handCount);
		if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, ModEntry.Instance.KokoroApi.Limited.Trait))
			return;
		combat.Queue(new Action { CardId = card.uuid, ArtifactPulseLate = Key() });
	}

	public bool ModifyLimitedUses(IKokoroApi.IV2.ILimitedApi.IHook.IModifyLimitedUsesArgs args)
	{
		args.Uses += GetExtraLimitedUses(args.Card);
		return false;
	}

	private sealed class Action : CardAction
	{
		public required int CardId;
		public string? ArtifactPulseLate;
		
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			if (s.FindCard(CardId) is not { } card)
				return;
			if (ModEntry.Instance.KokoroApi.Limited.GetLimitedUses(s, card) > 1)
				return;
			if (!c.exhausted.Contains(card))
				return;
			
			if (ArtifactPulseLate is not null)
				s.EnumerateAllArtifacts().FirstOrDefault(a => a.Key() == ArtifactPulseLate)?.Pulse();
			
			SetExtraLimitedUses(card, GetExtraLimitedUses(card) + 1);
		}
	}
}