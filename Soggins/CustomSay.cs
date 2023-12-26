namespace Shockah.Soggins;

internal sealed class CustomSay : Say
{
	private static int NextId = 1;

	public string? Text { get; set; }

	public override bool Execute(G g, IScriptTarget target, ScriptCtx ctx)
	{
		if (Text is null)
			return base.Execute(g, target, ctx);
		if (!string.IsNullOrEmpty(hash))
			return base.Execute(g, target, ctx);

		hash = $"{GetType().FullName}:{NextId++}";
		DB.currentLocale.strings[Say.GetLocKey(ctx.script, hash)] = Text;
		return base.Execute(g, target, ctx);
	}
}