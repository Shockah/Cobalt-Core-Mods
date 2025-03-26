using Nickel;

namespace Shockah.Destiny;

public sealed class ApiImplementation : IDestinyApi
{
	public IDeckEntry DestinyDeck
		=> ModEntry.Instance.DestinyDeck;

	public IStatusEntry MagicFindStatus
		=> MagicFind.MagicFindStatus;

	public ICardTraitEntry EnchantedTrait
		=> Enchanted.EnchantedTrait;
	
	public ICardTraitEntry ExplosiveTrait
		=> Explosive.ExplosiveTrait;

	public void RegisterHook(IDestinyApi.IHook hook, double priority = 0)
		=> ModEntry.Instance.HookManager.Register(hook, priority);

	public void UnregisterHook(IDestinyApi.IHook hook)
		=> ModEntry.Instance.HookManager.Unregister(hook);
}