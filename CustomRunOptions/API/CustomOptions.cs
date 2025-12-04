using System;
using System.Collections.Generic;
using Nickel.ModSettings;

namespace Shockah.CustomRunOptions;

public partial interface ICustomRunOptionsApi
{
	double SeedCustomRunOptionPriority { get; }
	double BootSequenceCustomRunOptionPriority { get; }
	double DailyModifiersCustomRunOptionPriority { get; }
	
	void RegisterCustomRunOption(ICustomRunOption option, double priority = 0);
	void UnregisterCustomRunOption(ICustomRunOption option);

	INewRunOptionsElement.IIcon MakeIconNewRunOptionsElement(Spr icon, int? width = null, int? height = null);
	INewRunOptionsElement.IArtifact MakeArtifactNewRunOptionsElement(Artifact artifact);

	IIconAffixModSetting MakeIconAffixModSetting(IModSettingsApi.IModSetting setting);
	IIconAffixModSetting.IIconSetting MakeIconAffixModSettingIconSetting();
	
	interface ICustomRunOption
	{
		IReadOnlyList<INewRunOptionsElement> GetNewRunOptionsElements(G g, RunConfig config);
		IModSettingsApi.IModSetting MakeCustomRunSettings(NewRunOptions baseRoute, G g, RunConfig config);
	}

	interface INewRunOptionsElement
	{
		Vec Size { get; }
		void Render(G g, Vec position);

		interface IIcon : INewRunOptionsElement
		{
			Spr Icon { get; set; }
			int? Width { get; set; }
			int? Height { get; set; }

			IIcon SetIcon(Spr value);
			IIcon SetWidth(int? value);
			IIcon SetHeight(int? value);
		}

		interface IArtifact : INewRunOptionsElement
		{
			Artifact Artifact { get; set; }

			IArtifact SetArtifact(Artifact value);
		}
	}

	interface IIconAffixModSetting : IModSettingsApi.IModSetting
	{
		IModSettingsApi.IModSetting Setting { get; set; }
		IIconSetting? LeftIcon { get; set; }
		IIconSetting? RightIcon { get; set; }
		Func<IEnumerable<Tooltip>>? Tooltips { get; set; }
		
		IIconAffixModSetting SetSetting(IModSettingsApi.IModSetting value);
		IIconAffixModSetting SetLeftIcon(IIconSetting? value);
		IIconAffixModSetting SetRightIcon(IIconSetting? value);
		IIconAffixModSetting SetTooltips(Func<IEnumerable<Tooltip>>? value);

		interface IIconSetting
		{
			Spr Icon { get; set; }
			int? IconWidth { get; set; }
			int? IconHeight { get; set; }
			int BoundsSpacing { get; set; }
			int ContentSpacing { get; set; }
			double VerticalAlignment { get; set; }
			
			IIconSetting SetIcon(Spr value);
			IIconSetting SetIconWidth(int? value);
			IIconSetting SetIconHeight(int? value);
			IIconSetting SetBoundsSpacing(int value);
			IIconSetting SetContentSpacing(int value);
			IIconSetting SetVerticalAlignment(double value);
		}
	}
}