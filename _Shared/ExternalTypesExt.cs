#if !IS_NICKEL_MOD
using CobaltCoreModding.Definitions.ExternalItems;

namespace Shockah.Shared;

internal static class ExternalTypesExt
{
	public static Spr Game(this ExternalSprite self)
		=> (Spr)self.Id!.Value;

	public static Deck Game(this ExternalDeck self)
		=> (Deck)self.Id!.Value;
}
#endif