using System.Collections.Generic;

namespace Shockah.Johnson;

internal sealed class CustomSay : Say
{
	private static int NextId = 1;

	private readonly int Id = 0;
	private int ExecuteCount = 0;

	public List<string>? AlternativeTexts;

	public CustomSay()
	{
		this.Id = NextId++;
	}

	public override bool Execute(G g, IScriptTarget target, ScriptCtx ctx)
	{
		var text = "";
		if (AlternativeTexts is not null && AlternativeTexts.Count != 0)
			text = AlternativeTexts[ExecuteCount % AlternativeTexts.Count];

		if (string.IsNullOrEmpty(text))
			return base.Execute(g, target, ctx);

		hash = $"{GetType().FullName}:{Id}";
		DB.currentLocale.strings[GetLocKey(ctx.script, hash)] = text;
		ExecuteCount++;
		return base.Execute(g, target, ctx);
	}
}