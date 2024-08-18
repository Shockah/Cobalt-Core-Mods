namespace Shockah.Kokoro;

public sealed class CustomCardBrowseManager
{
	private static ModEntry Instance => ModEntry.Instance;
	
	public ACardSelect MakeCustomCardBrowse(ACardSelect action, ICustomCardBrowseSource source)
	{
		var custom = Mutil.DeepCopy(action);
		custom.browseSource = (CardBrowse.Source)999999;
		Instance.Api.SetExtensionData(custom, "CustomCardBrowseSource", source);
		return custom;
	}
}