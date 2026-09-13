using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;

namespace Shockah.NewLight;

internal sealed class ScatterAction : AAttack, IRegisterable
{
	private static ISpriteEntry Icon = null!;

	public int Direction;
	
	[JsonProperty]
	private bool Scattered;

	public override Icon? GetIcon(State s)
	{
		if (base.GetIcon(s) is { } icon)
			return icon with { path = Icon.Sprite };
		return new(Icon.Sprite, damage, Colors.redd);
	}

	public override void Begin(G g, State s, Combat c)
	{
		if (Scattered)
		{
			var timer = this.timer;
			base.Begin(g, s, c);
			this.timer = timer;
			return;
		}

		if (Direction == 0)
		{
			var splitDamage = damage / 3;
			var leftoverDamage = damage % 3;
		
			var midAttack = Mutil.DeepCopy(this);
			midAttack.Scattered = true;
		
			var leftAttack = Mutil.DeepCopy(midAttack);
			var rightAttack = Mutil.DeepCopy(midAttack);

			midAttack.damage = splitDamage + leftoverDamage;
			leftAttack.damage = splitDamage;
			rightAttack.damage = splitDamage;

			midAttack.timer *= 0.5;
			leftAttack.timer *= 0.25;
			rightAttack.timer *= 0.25;
			
			// TODO: offset attacks
		
			c.QueueImmediate([leftAttack, rightAttack, midAttack]);
		}
		else
		{
			var splitDamage = damage / 2;
			var leftoverDamage = damage % 2;
		
			var midAttack = Mutil.DeepCopy(this);
			midAttack.Scattered = true;
			
			var sideAttack = Mutil.DeepCopy(midAttack);
			
			midAttack.damage = splitDamage + leftoverDamage;
			sideAttack.damage = splitDamage;
			
			midAttack.timer *= 0.75;
			sideAttack.timer *= 0.25;
			
			// TODO: offset attack
		
			c.QueueImmediate([sideAttack, midAttack]);
		}
		
		timer = 0;
	}

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		Icon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Actions/Scatter.png"));
	}
}