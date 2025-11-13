using System;
using Nickel;

namespace Shockah.CustomRunOptions;

public sealed class ApiImplementation(IModManifest mod) : ICustomRunOptionsApi
{
	public void RegisterBootSequenceUpside(string name, Func<string> title, Func<Choice, bool> matchPredicate)
	{
		var key = $"{mod.UniqueName}::{name}";
		BootSequenceCustomRunOption.UpsideChoices[key] = new BootSequenceCustomRunOption.IBootChoice.Modded(key, title, matchPredicate);
	}

	public void RegisterBootSequenceDownside(string name, Func<string> title, Func<Choice, bool> matchPredicate)
	{
		var key = $"{mod.UniqueName}::{name}";
		BootSequenceCustomRunOption.DownsideChoices[key] = new BootSequenceCustomRunOption.IBootChoice.Modded(key, title, matchPredicate);
	}
}