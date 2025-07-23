using Godot;
using static ResourceManager.Sounds;

public partial class GameButton : GameBaseButton
{
	public override void _Ready()
	{
		base._Ready();
		BHas_frame = false;
		TapSound.Stream = Sound_GraveButton;
	}

	public override void Bleep() { }
}
