using Microsoft.Xna.Framework;
using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;

namespace Shockah.Dracula;

internal interface IDraculaCard
{
	static abstract void Register(IPluginPackage<IModManifest> package, IModHelper helper);

	Vec ModifyTextCardScale(G g)
		=> Vec.One;

	Matrix ModifyNonTextCardRenderMatrix(G g, List<CardAction> actions)
		=> Matrix.Identity;

	Matrix ModifyCardActionRenderMatrix(G g, List<CardAction> actions, CardAction action, int actionWidth)
		=> Matrix.Identity;
}

internal interface IDraculaArtifact
{
	static abstract void Register(IModHelper helper);
}