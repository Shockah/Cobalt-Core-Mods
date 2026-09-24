using System;
using System.Collections.Generic;
using Nickel;

namespace Shockah.NewLight;

internal abstract class BaseStory
{
	protected static void InjectLocalizations(IModHelper helper, Action<Dictionary<string, string>> @delegate)
	{
		helper.Events.OnLoadStringsForLocale += (_, args) => @delegate(args.Localizations);
		if (helper.Events.ModLoadPhaseState.Phase >= ModLoadPhase.AfterDbInit)
			@delegate(DB.currentLocale.strings);
	}
}