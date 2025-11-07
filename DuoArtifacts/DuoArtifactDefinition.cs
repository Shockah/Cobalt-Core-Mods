using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.DuoArtifacts;

public sealed class DuoArtifactDefinition
{
	private static ModEntry Instance => ModEntry.Instance;

	public static readonly IReadOnlyList<DuoArtifactDefinition> Definitions = [
		new(typeof(BooksCatArtifact), [Deck.shard, Deck.colorless], I18n.BooksCatArtifactName, I18n.BooksCatArtifactTooltip, "BooksCat", DefinitionTooltip.Shard),
		new(typeof(BooksDizzyArtifact), [Deck.shard, Deck.dizzy], I18n.BooksDizzyArtifactName, I18n.BooksDizzyArtifactTooltip, "BooksDizzy", "status.shieldAlt", DefinitionTooltip.Shard),
		new(typeof(BooksDrakeArtifact), [Deck.shard, Deck.eunice], I18n.BooksDrakeArtifactName, I18n.BooksDrakeArtifactTooltip, "BooksDrake", DefinitionTooltip.Shard, "action.attackPiercing", "action.stun", "action.stunShip"),
		new(typeof(BooksIsaacArtifact), [Deck.shard, Deck.goat], I18n.BooksIsaacArtifactName, I18n.BooksIsaacArtifactTooltip, "BooksIsaac", DefinitionTooltip.Shard),
		new(typeof(BooksMaxArtifact), [Deck.shard, Deck.hacker], I18n.BooksMaxArtifactName, I18n.BooksMaxArtifactTooltip, "BooksMax", "cardtrait.exhaust", DefinitionTooltip.Shard),
		new(typeof(BooksPeriArtifact), [Deck.shard, Deck.peri], I18n.BooksPeriArtifactName, I18n.BooksPeriArtifactTooltip, "BooksPeri", DefinitionTooltip.Shard),
		new(typeof(BooksRiggsArtifact), [Deck.shard, Deck.riggs], I18n.BooksRiggsArtifactName, I18n.BooksRiggsArtifactTooltip, "BooksRiggs", new TTGlossary("status.hermes", 1), DefinitionTooltip.Shard),
		new(typeof(CatDizzyArtifact), [Deck.colorless, Deck.dizzy], I18n.CatDizzyArtifactName, I18n.CatDizzyArtifactTooltip, "CatDizzy", "status.shieldAlt", "status.perfectShield", I18n.MaxShieldLowerAltGlossary),
		new(typeof(CatDrakeArtifact), [Deck.colorless, Deck.eunice], I18n.CatDrakeArtifactName, I18n.CatDrakeArtifactTooltip, "CatDrake", "status.serenity", "status.timeStop"),
		new(typeof(CatIsaacArtifact), [Deck.colorless, Deck.goat], I18n.CatIsaacArtifactName, I18n.CatIsaacArtifactTooltip, "CatIsaac", "action.spawn"),
		new(typeof(CatMaxArtifact), [Deck.colorless, Deck.hacker], I18n.CatMaxArtifactName, I18n.CatMaxArtifactTooltip, "CatMax"),
		new(typeof(CatPeriArtifact), [Deck.colorless, Deck.peri], I18n.CatPeriArtifactName, I18n.CatPeriArtifactTooltip, "CatPeri", "cardtrait.temporary", "status.overdriveAlt"),
		new(typeof(CatRiggsArtifact), [Deck.colorless, Deck.riggs], I18n.CatRiggsArtifactName, I18n.CatRiggsArtifactTooltip, "CatRiggs", new TTGlossary("cardtrait.discount", 1)),
		new(typeof(DizzyDrakeArtifact), [Deck.dizzy, Deck.eunice], I18n.DizzyDrakeArtifactName, I18n.DizzyDrakeArtifactTooltip, "DizzyDrake", "action.overheat", "status.shieldAlt"),
		new(typeof(DizzyIsaacArtifact), [Deck.dizzy, Deck.goat], I18n.DizzyIsaacArtifactName, I18n.DizzyIsaacArtifactTooltip, "DizzyIsaac", "action.spawn", new(() => StatusMeta.GetTooltips(Instance.KokoroApi.OxidationStatus.Status, 1)[0]), "status.corrodeAlt"),
		new(typeof(DizzyMaxArtifact), [Deck.dizzy, Deck.hacker], I18n.DizzyMaxArtifactName, I18n.DizzyMaxArtifactTooltip, "DizzyMax", "status.shieldAlt", new TTGlossary("status.boost", 1)),
		new(typeof(DizzyPeriArtifact), [Deck.dizzy, Deck.peri], I18n.DizzyPeriArtifactName, I18n.DizzyPeriArtifactTooltip, "DizzyPeri", "status.shieldAlt", "status.overdriveAlt"),
		new(typeof(DizzyRiggsArtifact), [Deck.dizzy, Deck.riggs], I18n.DizzyRiggsArtifactName, I18n.DizzyRiggsArtifactTooltip, "DizzyRiggs", "status.shieldAlt", "status.evade"),
		new(typeof(DrakeIsaacArtifact), [Deck.eunice, Deck.goat], I18n.DrakeIsaacArtifactName, I18n.DrakeIsaacArtifactTooltip, "DrakeIsaac", I18n.HeatAltGlossary),
		new(typeof(DrakeMaxArtifact), [Deck.eunice, Deck.hacker], I18n.DrakeMaxArtifactName, I18n.DrakeMaxArtifactTooltip, "DrakeMax"),
		new(typeof(DrakePeriArtifact), [Deck.eunice, Deck.peri], I18n.DrakePeriArtifactName, I18n.DrakePeriArtifactTooltip, "DrakePeri", "action.overheat", "status.overdriveAlt", "status.powerdriveAlt"),
		new(typeof(DrakeRiggsArtifact), [Deck.eunice, Deck.riggs], I18n.DrakeRiggsArtifactName, I18n.DrakeRiggsArtifactTooltip, "DrakeRiggs", "status.evade", I18n.HeatAltGlossary),
		new(typeof(IsaacMaxArtifact), [Deck.goat, Deck.hacker], I18n.IsaacMaxArtifactName, I18n.IsaacMaxArtifactTooltip, "IsaacMax", "cardtrait.exhaust", "midrow.bubbleShield", "action.spawn", "midrow.asteroid"),
		new(typeof(IsaacPeriArtifact), [Deck.goat, Deck.peri], I18n.IsaacPeriArtifactName, I18n.IsaacPeriArtifactTooltip, "IsaacPeri", I18n.FluxAltGlossary, "status.overdriveAlt", "status.powerdriveAlt"),
		new(typeof(IsaacRiggsArtifact), [Deck.goat, Deck.riggs], I18n.IsaacRiggsArtifactName, I18n.IsaacRiggsArtifactTooltip, "IsaacRiggs", "status.droneShift", "status.evade"),
		new(typeof(MaxPeriArtifact), [Deck.hacker, Deck.peri], I18n.MaxPeriArtifactName, I18n.MaxPeriArtifactTooltip, "MaxPeri"),
		new(typeof(MaxRiggsArtifact), [Deck.hacker, Deck.riggs], I18n.MaxRiggsArtifactName, I18n.MaxRiggsArtifactTooltip, "MaxRiggs"),
		new(typeof(PeriRiggsArtifact), [Deck.peri, Deck.riggs], I18n.PeriRiggsArtifactName, I18n.PeriRiggsArtifactTooltip, "PeriRiggs", new TTGlossary("status.strafe", 1), "status.evade"),
	];

	private static readonly Dictionary<Type, DuoArtifactDefinition> TypeToDefinitionDictionary = [];

	static DuoArtifactDefinition()
	{
		foreach (var definition in Definitions)
			TypeToDefinitionDictionary[definition.Type] = definition;
	}

	public static DuoArtifactDefinition? GetDefinition(Type type)
		=> type.IsAssignableTo(typeof(DuoArtifact)) ? TypeToDefinitionDictionary.GetValueOrDefault(type) : null;

	public static DuoArtifactDefinition? GetDefinition<TType>() where TType : DuoArtifact
		=> TypeToDefinitionDictionary.GetValueOrDefault(typeof(TType));

	public readonly Type Type;
	public readonly IReadOnlySet<Deck> Characters;
	public readonly string Name;
	public readonly string Tooltip;
	public readonly string AssetName;
	public readonly IReadOnlyList<DefinitionTooltip> ExtraTooltips;

	internal readonly Lazy<HashSet<string>> CharacterKeys;

	public DuoArtifactDefinition(Type type, IEnumerable<Deck> characters, string name, string tooltip, string assetName, params DefinitionTooltip[] extraTooltips)
	{
		this.Type = type;
		this.Characters = characters.ToHashSet();
		this.Name = name;
		this.Tooltip = tooltip;
		this.AssetName = assetName;
		this.ExtraTooltips = extraTooltips;
		this.CharacterKeys = new(() => this.Characters.Select(c => c.Key()).ToHashSet());
	}

	public sealed class DefinitionTooltip
	{
		public static DefinitionTooltip Shard
			=> new(() =>
			{
				var maxAmount = (MG.inst.g?.state ?? DB.fakeState).ship.Get(Status.maxShard);
				if (maxAmount == 0)
					maxAmount = 3;
				return new TTGlossary($"status.{Status.shard.Key()}", maxAmount);
			});

		private readonly Func<Tooltip> TooltipFactory;

		public DefinitionTooltip(Func<DefinitionTooltip> lazyFunction)
		{
			this.TooltipFactory = () => lazyFunction().MakeTooltip();
		}

		public DefinitionTooltip(Tooltip tooltip)
		{
			this.TooltipFactory = () => tooltip;
		}

		public DefinitionTooltip(Status status, int @default)
		{
			this.TooltipFactory = () =>
			{
				var amount = (MG.inst.g?.state ?? DB.fakeState).ship.Get(status);
				if (amount == 0)
					amount = @default;
				return new TTGlossary($"status.{status.Key()}", amount);
			};
		}

		public DefinitionTooltip(string glossaryKey) : this(new TTGlossary(glossaryKey)) { }

		public Tooltip MakeTooltip()
			=> TooltipFactory();

		public static implicit operator DefinitionTooltip(Func<DefinitionTooltip> lazyFunction)
			=> new(lazyFunction);

		public static implicit operator DefinitionTooltip(Tooltip tooltip)
			=> new(tooltip);

		public static implicit operator DefinitionTooltip(string glossaryKey)
			=> new(glossaryKey);
	}
}