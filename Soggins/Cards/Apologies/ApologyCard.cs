using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.IO;

namespace Shockah.Soggins;

[CardMeta(dontOffer = true, rarity = Rarity.common)]
public abstract class ApologyCard : Card, IFrogproofCard
{
	private static ModEntry Instance => ModEntry.Instance;

	protected static ExternalSprite Art { get; private set; } = null!;

	public string? ApologyFlavorText;

	public virtual void RegisterArt(ISpriteRegistry registry)
	{
		if (Art is not null)
			return;

		Art = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.CardArt.Apology",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "CardArt", "Apology.png"))
		);
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 0,
			temporary = true,
			exhaust = true,
			art = (Spr)Art.Id!.Value
		};

	public virtual double GetApologyWeight(State state, Combat combat, int timesGiven)
		=> 1.0 / (timesGiven + 1);
}
