using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Dracula;
using Shockah.Kokoro;

namespace Shockah.Bjorn;

internal sealed class GadgetManager : IRegisterable
{
	private const int MaxStacks = 15;
	
	internal static IStatusEntry GadgetStatus { get; private set; } = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		GadgetStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("Gadget", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Statuses/Gadget.png")).Sprite,
				color = new("B9B9B9"),
				isGood = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "Gadget", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "Gadget", "description"]).Localize
		});
		
		ModEntry.Instance.Helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnCombatEnd), (State state) =>
		{
			var stacks = state.ship.Get(GadgetStatus.Status);
			switch (stacks)
			{
				case > 0:
					ModEntry.Instance.Helper.ModData.SetModData(state, "GadgetProgress", stacks);
					break;
				default:
					ModEntry.Instance.Helper.ModData.RemoveModData(state, "GadgetProgress");
					break;
			}
		});

		ModEntry.Instance.Helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnCombatStart), (State state) =>
		{
			state.ship.Set(GadgetStatus.Status, ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(state, "GadgetProgress"));
		});

		ModEntry.Instance.Helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnTurnStart), (State state, Combat combat) =>
		{
			if (combat.turn != 1)
				return;
			if (!IsAtLastCombatNode(state))
				return;
			if (ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(state, "GadgetProgress") <= 0)
				return;
			
			ModEntry.Instance.Helper.ModData.SetModData(combat, "CreatedTerminate", true);
			combat.Queue(new AAddCard { destination = CardDestination.Hand, card = new TerminateCard() });
		});
		
		ModEntry.Instance.Helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.AfterPlayerStatusAction), (State state, Combat combat, Status status) =>
		{
			if (status != GadgetStatus.Status)
				return;
			Run();

			void Run()
			{
				while (true)
				{
					var progressAtCombatStart = ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(state, "GadgetProgress");
					var finishedGadgetThisCombat = ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(combat, "GadgetFinished");
					var currentMaxStacks = finishedGadgetThisCombat ? (IsAtLastCombatNode(state) ? 0 : progressAtCombatStart) : (MaxStacks + progressAtCombatStart);
					var gadgetProgress = state.ship.Get(GadgetStatus.Status);

					if (gadgetProgress > currentMaxStacks)
					{
						state.ship.Add(GadgetStatus.Status, -(gadgetProgress - currentMaxStacks));
						combat.QueueImmediate(new SmartShieldAction { TargetPlayer = true, Amount = gadgetProgress - currentMaxStacks });
					}

					if (!finishedGadgetThisCombat && gadgetProgress >= MaxStacks)
					{
						state.ship.Add(GadgetStatus.Status, -MaxStacks);
						combat.QueueImmediate(new AArtifactOffering { amount = 2, limitPools = [ArtifactPool.Common] });
						ModEntry.Instance.Helper.ModData.SetModData(combat, "GadgetFinished", true);

						if (IsAtLastCombatNode(state))
							continue;
					}

					if (gadgetProgress is > 0 and < MaxStacks && IsAtLastCombatNode(state) && !ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(combat, "CreatedTerminate"))
					{
						ModEntry.Instance.Helper.ModData.SetModData(combat, "CreatedTerminate", true);
						combat.Queue(new AAddCard { destination = CardDestination.Hand, card = new TerminateCard() });
					}

					break;
				}
			}
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(State), nameof(State.PopulateRun)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(State_PopulateRun_Postfix))
		);
		
		ModEntry.Instance.KokoroApi.StatusRendering.RegisterHook(new GadgetStatusRenderingHook());
		
		helper.ModRegistry.AwaitApi<IDraculaApi>(
			"Shockah.Dracula",
			api => api.RegisterBloodTapOptionProvider(GadgetStatus.Status, (_, _, status) => [
				new AHurt { targetPlayer = true, hurtAmount = 1 },
				new AStatus { targetPlayer = true, status = status, statusAmount = 2 },
			])
		);
	}

	private static bool IsAtLastCombatNode(State state)
		=> !state.IsOutsideRun() && state.map.IsFinalZone() && state.map.GetCurrent().contents is MapBattle { battleType: BattleType.Boss };

	private static void State_PopulateRun_Postfix(State __instance)
		=> ModEntry.Instance.Helper.ModData.RemoveModData(__instance, "GadgetProgress");

	private sealed class GadgetStatusRenderingHook : IKokoroApi.IV2.IStatusRenderingApi.IHook
	{
		public IKokoroApi.IV2.IStatusRenderingApi.IStatusInfoRenderer? OverrideStatusInfoRenderer(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusInfoRendererArgs args)
		{
			if (args.Status != GadgetStatus.Status)
				return null;

			var progressAtCombatStart = ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(args.State, "GadgetProgress");
			var finishedGadgetThisCombat = ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(args.Combat, "GadgetFinished");
			
			var colors = new Color[MaxStacks];
			for (var i = 0; i < colors.Length; i++)
				colors[i] = GetColor(i);

			return ModEntry.Instance.KokoroApi.StatusRendering.MakeBarStatusInfoRenderer().SetSegments(colors).SetRows(3);

			Color GetColor(int i)
			{
				if (finishedGadgetThisCombat && i >= progressAtCombatStart)
					return Colors.downside;
				if (i >= args.Amount)
					return ModEntry.Instance.KokoroApi.StatusRendering.DefaultInactiveStatusBarColor;
				if (!finishedGadgetThisCombat && i < progressAtCombatStart)
					return Colors.cheevoGold;
				return ModEntry.Instance.KokoroApi.StatusRendering.DefaultActiveStatusBarColor;
			}
		}
	}
}