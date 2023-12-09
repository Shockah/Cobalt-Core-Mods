using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Kokoro;

internal sealed class MidrowTracker : FX
{
	public sealed class MidrowObjectEntry
	{
		public readonly StuffBase Object;
		public readonly SerializableMidrowObjectEntry Serializable;

		public int X
		{
			get => Serializable.X;
			set => Serializable.X = value;
		}

		public Dictionary<string, object?> Tags
			=> Serializable.Tags;

		public MidrowObjectEntry(StuffBase @object)
		{
			this.Object = @object;
			this.Serializable = new(@object.x);
		}

		public MidrowObjectEntry(StuffBase @object, SerializableMidrowObjectEntry serializedEntry)
		{
			if (@object.x != serializedEntry.X)
				throw new ArgumentException();
			this.Object = @object;
			this.Serializable = serializedEntry;
		}
	}

	public sealed class SerializableMidrowObjectEntry
	{
		public readonly Guid ID;
		public int X;
		public readonly Dictionary<string, object?> Tags = new();

		public SerializableMidrowObjectEntry(int x) : this(Guid.NewGuid(), x, new()) { }

		[JsonConstructor]
		public SerializableMidrowObjectEntry(Guid id, int x, Dictionary<string, object?> tags)
		{
			this.ID = id;
			this.X = x;
			this.Tags = tags;
		}
	}

	[JsonIgnore]
	private readonly Dictionary<StuffBase, MidrowObjectEntry> EntriesByObject = new();

	[JsonProperty]
	private readonly List<SerializableMidrowObjectEntry> SerializableEntries = new();

	[JsonIgnore]
	private bool FixedAfterDeserialiaztion = false;

	public static MidrowTracker ObtainMidrowTracker(Combat combat)
	{
		var tracker = combat.fx.OfType<MidrowTracker>().FirstOrDefault();
		if (tracker is null)
		{
			tracker = new();
			combat.fx.Add(tracker);
		}
		tracker.FixAfterDeserialization(combat);
		tracker.Update(combat);
		return tracker;
	}

	public override void Update(G g)
	{
		base.Update(g);
		age = 0.1; // resetting age so it doesn't get removed at least until end of combat
	}

	public MidrowObjectEntry ObtainEntry(StuffBase @object)
	{
		if (!EntriesByObject.TryGetValue(@object, out var entry))
			entry = AddObject(@object);
		return entry;
	}

	public void Update(Combat combat)
	{
		var oldObjects = EntriesByObject.Values.Select(e => e.Object).ToHashSet();
		var newObjects = combat.stuff.Values.ToHashSet();

		var addedObjects = newObjects.Where(o => !oldObjects.Contains(o)).ToHashSet();
		var removedObjects = oldObjects.Where(o => !newObjects.Contains(o)).ToHashSet();
		var existingObjects = newObjects.Where(oldObjects.Contains).ToHashSet();

		foreach (var @object in removedObjects)
			RemoveObject(@object);
		foreach (var @object in addedObjects)
			AddObject(@object);

		foreach (var @object in existingObjects)
		{
			if (!EntriesByObject.TryGetValue(@object, out var entry))
				throw new ArgumentException();
			entry.X = @object.x;
		}
	}

	private MidrowObjectEntry AddObject(StuffBase @object)
	{
		MidrowObjectEntry entry = new(@object);
		EntriesByObject[@object] = entry;
		SerializableEntries.Add(entry.Serializable);
		return entry;
	}

	private void RemoveObject(StuffBase @object)
	{
		if (!EntriesByObject.TryGetValue(@object, out var entry))
			return;
		EntriesByObject.Remove(@object);
		SerializableEntries.Remove(entry.Serializable);
	}

	private void FixAfterDeserialization(Combat combat)
	{
		if (FixedAfterDeserialiaztion)
			return;
		EntriesByObject.Clear();

		List<SerializableMidrowObjectEntry> invalidEntries = new();
		foreach (var serializedEntry in SerializableEntries)
		{
			if (!combat.stuff.TryGetValue(serializedEntry.X, out var @object))
			{
				invalidEntries.Add(serializedEntry);
				continue;
			}

			MidrowObjectEntry entry = new(@object, serializedEntry);
			EntriesByObject[@object] = entry;
		}

		foreach (var invalidEntry in invalidEntries)
			SerializableEntries.Remove(invalidEntry);
		FixedAfterDeserialiaztion = true;
	}
}