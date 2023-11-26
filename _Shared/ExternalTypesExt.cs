using CobaltCoreModding.Definitions.ExternalItems;

namespace Shockah.Shared;

public static class ExternalTypesExt
{
	public static Spr Game(this ExternalSprite self)
		=> (Spr)self.Id!.Value;

	public static Deck Game(this ExternalDeck self)
		=> (Deck)self.Id!.Value;
}