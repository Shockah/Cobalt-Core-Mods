namespace Shockah.MORE;

internal sealed class CustomSay : Say
{
	private static int NextId = 1;

	private readonly int Id = 0;

	public string? Text;

	public CustomSay()
	{
		this.Id = NextId++;
	}

	public override bool Execute(G g, IScriptTarget target, ScriptCtx ctx)
	{
		if (string.IsNullOrEmpty(Text))
			return base.Execute(g, target, ctx);

		hash = $"{GetType().FullName}:{Id}";
		DB.currentLocale.strings[GetLocKey(ctx.script, hash)] = Text;
		return base.Execute(g, target, ctx);
	}
}