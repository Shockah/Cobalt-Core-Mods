using System.Collections.Generic;
using System.Linq;

namespace Shockah.Bloch;

file static class ObjectExt
{
	public static string ToNiceString(this object? o)
	{
		if (o is IEnumerable<object> enumerable)
			return $"[{string.Join(", ", enumerable.Select(o2 => o2.ToNiceString()))}]";
		return o?.ToString() ?? "<null>";
	}
}

internal sealed class LocalizedSay : Say
{
	public required List<string> Key;

	public override bool Execute(G g, IScriptTarget target, ScriptCtx ctx)
	{
		hash = $"{GetType().FullName}:{Key.ToNiceString()}";
		DB.currentLocale.strings[GetLocKey(ctx.script, hash)] = ModEntry.Instance.DialogueLocalizations.Localize(Key);
		return base.Execute(g, target, ctx);
	}
}