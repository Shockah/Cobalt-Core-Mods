using System.Collections.Generic;
using System.Reflection;
using FSPRO;
using Nanoray.PluginManager;
using Nickel;
using TheJazMaster.TyAndSasha;

namespace Shockah.Destiny;

internal sealed class DestinyTyArtifact : Artifact, IRegisterable, IDestinyApi.IHook
{
	private static ITyAndSashaApi TyAndSashaApi = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;
		if (ModEntry.Instance.Helper.ModRegistry.GetApi<ITyAndSashaApi>("TheJazMaster.TyAndSasha") is not { } tyAndSashaApi)
			return;

		TyAndSashaApi = tyAndSashaApi;
		
		helper.Content.Artifacts.RegisterArtifact("DestinyTy", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/Ty.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Ty", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Ty", "description"]).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.DestinyDeck.Deck, tyAndSashaApi.TyDeck]);
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [
			.. Enchanted.EnchantedTrait.Configuration.Tooltips?.Invoke(DB.fakeState, null) ?? [],
			.. TyAndSashaApi.WildTrait.Configuration.Tooltips?.Invoke(DB.fakeState, null) ?? [],
		];

	public bool? OnEnchant(IDestinyApi.IHook.IOnEnchantArgs args)
	{
		if (args.EnchantLevel < args.MaxEnchantLevel)
			return null;
		if (args.FromUserInteraction && args.State.CharacterIsMissing(args.Card.GetMeta().deck))
			return null;
		if (ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(args.State, args.Card, TyAndSashaApi.WildTrait))
			return null;
		
		var environment = ModEntry.Instance.KokoroApi.ActionCosts.MakeStatePaymentEnvironment(args.State, args.Combat, args.Card);
		var cost = ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(Status.shard), 1);
		var transaction = ModEntry.Instance.KokoroApi.ActionCosts.GetBestTransaction(cost, environment);
		var transactionPaymentResult = transaction.TestPayment(environment);

		if (transactionPaymentResult.UnpaidResources.Count != 0)
		{
			if (args.FromUserInteraction)
			{
				args.Card.shakeNoAnim = 1.0;
				Audio.Play(Event.ZeroEnergy);
			}
			return false;
		}

		transaction.Pay(environment);
		ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(args.State, args.Card, TyAndSashaApi.WildTrait, true, false);
		
		if (args.FromUserInteraction)
		{
			args.Card.flipAnim = 1;
			Audio.Play(Event.Status_PowerUp);
		}
		
		return true;
	}
}