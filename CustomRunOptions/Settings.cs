using System;
using Newtonsoft.Json;
using Nickel;
using Nickel.ModSettings;

namespace Shockah.CustomRunOptions;

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
	[JsonProperty]
	public double SoloDailyChance = 0.6;
	
	[JsonProperty]
	public double DuoDailyChance;
	
	[JsonProperty]
	public double UnmannedDailyChance;
	
	[JsonProperty]
	public double SoloRandomizeChance;
	
	[JsonProperty]
	public double DuoRandomizeChance;
	
	[JsonProperty]
	public double UnmannedRandomizeChance;
}