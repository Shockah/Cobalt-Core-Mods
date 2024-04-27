//using System;
//using System.Collections.Generic;

//namespace EvilRiggs.Artifacts;

//internal class PerpetualSpeedDevice : Artifact
//{
//	public bool triggered;

//	public override void OnCombatStart(State state, Combat combat)
//	{
//		triggered = false;
//	}

//	public override void OnTurnStart(State state, Combat combat)
//	{
//		triggered = false;
//	}

//	public override void OnCombatEnd(State state)
//	{
//		triggered = false;
//	}

//	public override void OnQueueEmptyDuringPlayerTurn(State state, Combat combat)
//	{
//		if (combat.hand.Count == 0 && !triggered)
//		{
//			triggered = true;
//			combat.Queue((CardAction)new AStatus
//			{
//				status = (Status)11,
//				targetPlayer = true,
//				statusAmount = 3,
//				artifactPulse = ((Artifact)this).Key()
//			});
//		}
//	}

//	public override Spr GetSprite()
//	{
//		string spr = (triggered ? "artifact_perpetualSpeedDeviceUsed" : "artifact_perpetualSpeedDevice");
//		return (Spr)Manifest.sprites[spr].Id!.Value;
//	}

//	public override List<Tooltip>? GetExtraTooltips()
//	{
//		List<Tooltip> list = new List<Tooltip>();
//		list.Add((Tooltip)new TTGlossary("status.evade", Array.Empty<object>()));
//		return list;
//	}
//}
