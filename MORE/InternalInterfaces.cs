﻿using Nanoray.PluginManager;
using Nickel;

namespace Shockah.MORE;

internal interface IRegisterable
{
	static abstract void Register(IPluginPackage<IModManifest> package, IModHelper helper);
	static virtual void UpdateSettings(IPluginPackage<IModManifest> package, IModHelper helper, ProfileSettings settings) { }
}