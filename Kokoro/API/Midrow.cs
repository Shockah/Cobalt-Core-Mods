using System.Diagnostics.CodeAnalysis;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	void TagMidrowObject(Combat combat, StuffBase @object, string tag, object? tagValue = null);
	void UntagMidrowObject(Combat combat, StuffBase @object, string tag);
	bool IsMidrowObjectTagged(Combat combat, StuffBase @object, string tag);
	bool TryGetMidrowObjectTag(Combat combat, StuffBase @object, string tag, [MaybeNullWhen(false)] out object? tagValue);
}