using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[CardMeta(deck = Deck.colorless, rarity = Rarity.common, upgradesTo = [Upgrade.A, Upgrade.B])]
public sealed class SogginsExeCard : Card, IRegisterableCard, IFrogproofCard
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Art = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Art = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.CardArt.SogginsExe",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "CardArt", "SogginsExe.png"))
		);
	}

	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.SogginsExe",
			cardType: GetType(),
			cardArt: Art,
			actualDeck: ExternalDeck.GetRaw((int)Deck.colorless)
		);
		card.AddLocalisation(I18n.ExeCardName);
		registry.RegisterCard(card);
	}

	public void InjectDialogue()
	{
		DB.story.all[$"{Key()}_0"] = new()
		{
			type = NodeType.combat,
			allPresent = ["comp"],
			lookup = [$"summon{Instance.SogginsDeck.GlobalName}"],
			oncePerCombatTags = [$"summon{Instance.SogginsDeck.GlobalName}Tag"],
			oncePerRun = true,
			lines = [
				new CustomSay()
				{
					who = "comp",
					Text = "I can feel my CPU slowing down.",
					loopTag = "squint"
				}
			]
		};
		DB.story.all[$"{Key()}_1"] = new()
		{
			type = NodeType.combat,
			allPresent = ["comp"],
			lookup = [$"summon{Instance.SogginsDeck.GlobalName}"],
			oncePerCombatTags = [$"summon{Instance.SogginsDeck.GlobalName}Tag"],
			oncePerRun = true,
			lines = [
				new CustomSay()
				{
					who = "comp",
					Text = "The smugness is unbearable.",
					loopTag = "squint"
				}
			]
		};
	}

	private int GetCost()
		=> upgrade switch
		{
			Upgrade.A => 0,
			Upgrade.B => 1,
			_ => 1
		};

	private string GetText()
		=> upgrade switch
		{
			Upgrade.A => I18n.ExeCardTextA,
			Upgrade.B => I18n.ExeCardTextB,
			_ => I18n.ExeCardText0,
		};

	private int GetChoiceCount()
		=> upgrade switch
		{
			Upgrade.A => 2,
			Upgrade.B => 3,
			_ => 2
		};

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.artTint = I18n.SogginsColor;
		data.cost = GetCost();
		data.description = GetText();
		data.exhaust = true;
		return data;
	}

	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> actions = [];
		if (!Instance.Api.IsSmugEnabled(s, s.ship))
			actions.Add(new AEnableSmug());

		switch (upgrade)
		{
			case Upgrade.A:
				actions.Add(new AStatus
				{
					status = (Status)Instance.SmugStatus.Id!.Value,
					statusAmount = 1,
					targetPlayer = true
				});
				break;
			case Upgrade.B:
				actions.Add(new AStatus
				{
					status = (Status)Instance.SmugStatus.Id!.Value,
					mode = AStatusMode.Set,
					statusAmount = 0,
					targetPlayer = true
				});
				break;
			default:
				break;
		}

		actions.Add(new ASogginsExe
		{
			amount = GetChoiceCount(),
			limitDeck = (Deck)Instance.SogginsDeck.Id!.Value,
			makeAllCardsTemporary = true,
			overrideUpgradeChances = false,
			canSkip = false,
			inCombat = true,
			discount = -1,
			dialogueSelector = $".summon{Instance.SogginsDeck.GlobalName}"
		});

		return actions;
	}
}
