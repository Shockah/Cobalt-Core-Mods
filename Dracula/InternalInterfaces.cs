﻿using Microsoft.Xna.Framework;
using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;

namespace Shockah.Dracula;

internal interface IRegisterable
{
	static abstract void Register(IPluginPackage<IModManifest> package, IModHelper helper);
}

internal interface IDraculaCard : IRegisterable
{
	Matrix ModifyNonTextCardRenderMatrix(G g, IReadOnlyList<CardAction> actions)
		=> Matrix.Identity;

	Matrix ModifyCardActionRenderMatrix(G g, IReadOnlyList<CardAction> actions, CardAction action, int actionWidth)
		=> Matrix.Identity;
}