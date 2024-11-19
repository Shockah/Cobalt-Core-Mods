using Shockah.Kokoro;

namespace Shockah.Soggins;

internal sealed class StatusLogicManager : IKokoroApi.IV2.IStatusLogicApi.IHook
{
	private static ModEntry Instance => ModEntry.Instance;

	internal StatusLogicManager()
	{
		Instance.KokoroApi.StatusLogic.RegisterHook(this);
	}

	public bool? IsAffectedByBoost(IKokoroApi.IV2.IStatusLogicApi.IHook.IIsAffectedByBoostArgs args)
		=> args.Status == (Status)Instance.DoubleTimeStatus.Id!.Value || args.Status == (Status)Instance.BotchesStatus.Id!.Value || args.Status == (Status)Instance.SmugStatus.Id!.Value ? false : null;
}
