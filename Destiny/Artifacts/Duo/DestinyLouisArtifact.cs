using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using TheJazMaster.Louis;

namespace Shockah.Destiny;

internal sealed class DestinyLouisArtifact : Artifact, IRegisterable, IDestinyApi.IHook
{
	private static ILouisApi LouisApi = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;
		if (ModEntry.Instance.Helper.ModRegistry.GetApi<ILouisApi>("TheJazMaster.Louis") is not { } louisApi)
			return;

		LouisApi = louisApi;
		
		helper.Content.Artifacts.RegisterArtifact("DestinyLouis", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/Louis.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Louis", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Louis", "description"]).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.DestinyDeck.Deck, louisApi.LouisDeck]);
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [
			.. Enchanted.EnchantedTrait.Configuration.Tooltips?.Invoke(DB.fakeState, null) ?? [],
			.. StatusMeta.GetTooltips(Status.tempShield, 1),
			.. LouisApi.GemTrait.Configuration.Tooltips?.Invoke(DB.fakeState, null) ?? [],
		];

	public void AfterEnchant(IDestinyApi.IHook.IAfterEnchantArgs args)
	{
		var gems = LouisApi.GemHandCount(args.State, args.Combat);
		if (gems <= 0)
			return;
		
		args.Combat.QueueImmediate(new AStatus { targetPlayer = true, status = Status.tempShield, statusAmount = gems, artifactPulse = Key() });
	}
}