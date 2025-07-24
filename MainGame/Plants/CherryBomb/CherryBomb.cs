using Godot;
using Godot.Collections;

public partial class CherryBomb : Plants
{
	[Export]private AnimationPlayer _animExplode;
	[Export]private int _damage = 1800;
	public override void _Idle()
	{
		Explode();
	}

	public void Explode()
	{
		
		_animExplode.Play("CherryBomb_explode");
		_animExplode.AnimationFinished += DamageZombies;
}

	private void DamageZombies(StringName animName)
	{
		GD.Print("CherryBomb exploded!");
		Area2D area = GetNode<Area2D>("Area2D");
		Array<Area2D> overlappingAreas = area.GetOverlappingAreas();
		foreach (Area2D overlappingArea in overlappingAreas)
		{
			GD.Print("CherryBomb overlapping area: " + overlappingArea.Name);
			if (overlappingArea.GetNode("../..") is Zombie zombie)
			{
				GD.Print("CherryBomb damaging zombie!");
				//僵尸扣血
				zombie.Hurt(new Hurt(_damage, HurtType.Explosion));
			}
		}
		Visible = false;
		FreePlant();
	}
}
