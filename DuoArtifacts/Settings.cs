using Newtonsoft.Json;
using Nickel;
using System;
using Newtonsoft.Json.Converters;

namespace Shockah.DuoArtifacts;

internal sealed class Settings
{
	[JsonProperty]
	public ProfileSettings Global = new();

	[JsonIgnore]
	public ProfileBasedValue<IModSettingsApi.ProfileMode, ProfileSettings> ProfileBased;

	public Settings()
	{
		this.ProfileBased = ProfileBasedValue.Create(
			() => ModEntry.Instance.Helper.ModData.GetModDataOrDefault(MG.inst.g?.state ?? DB.fakeState, "ActiveProfile", IModSettingsApi.ProfileMode.Slot),
			profile => ModEntry.Instance.Helper.ModData.SetModData(MG.inst.g?.state ?? DB.fakeState, "ActiveProfile", profile),
			profile => profile switch
			{
				IModSettingsApi.ProfileMode.Global => this.Global,
				IModSettingsApi.ProfileMode.Slot => ModEntry.Instance.Helper.ModData.ObtainModData<ProfileSettings>(MG.inst.g?.state ?? DB.fakeState, "ProfileSettings"),
				_ => throw new ArgumentOutOfRangeException(nameof(profile), profile, null)
			},
			(profile, data) =>
			{
				switch (profile)
				{
					case IModSettingsApi.ProfileMode.Global:
						this.Global = data;
						break;
					case IModSettingsApi.ProfileMode.Slot:
						ModEntry.Instance.Helper.ModData.SetModData(MG.inst.g?.state ?? DB.fakeState, "ProfileSettings", data);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		);
	}
}

internal sealed class ProfileSettings
{
	public OfferingModeEnum OfferingMode = OfferingModeEnum.ExtraOnceThenCommon;
	
	public bool ArtifactsCondition = true;
	public int MinArtifacts = 1;

	public bool RareCardsCondition = true;
	public int MinRareCards = 1;

	public bool AnyCardsCondition = true;
	public int MinCards = 5;

	public bool BootSequenceUpsideEnabled = true;
	public int BootSequenceUpsideCards = 6;

	[JsonConverter(typeof(StringEnumConverter))]
	public enum OfferingModeEnum
	{
		Common,
		Extra,
		ExtraOnceThenCommon,
	}
}