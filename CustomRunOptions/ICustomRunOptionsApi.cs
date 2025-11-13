using System;

namespace Shockah.CustomRunOptions;

public interface ICustomRunOptionsApi
{
	void RegisterBootSequenceUpside(string name, Func<string> title, Func<Choice, bool> matchPredicate);
	void RegisterBootSequenceDownside(string name, Func<string> title, Func<Choice, bool> matchPredicate);
}