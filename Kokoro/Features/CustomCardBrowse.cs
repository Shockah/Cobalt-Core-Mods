namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class ActionApiImplementation
	{
		public ACardSelect MakeCustomCardBrowse(ACardSelect action, ICustomCardBrowseSource source)
			=> Instance.CustomCardBrowseManager.MakeCustomCardBrowse(action, source);
	}
}

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