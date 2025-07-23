using Godot;
using System;
using static ResourceManager.Sounds;

public partial class GameBaseButton : Sprite2D
{
	public Vector2 Pos = new();
	public bool BCan_move = true;
	public bool BPicked = false;
	public bool BHas_frame = true;
	public AudioStreamPlayer BleepSound = new();
	public AudioStreamPlayer TapSound = new();
	[Export] MainNode2D _main;
	public override void _Ready()
	{
		Pos = Position;
		

		BleepSound.Stream = Sound_Bleep;
		TapSound.Stream = Sound_Tap;

		AddChild(BleepSound);
		AddChild(TapSound);
	}

	private void OnMouseEntered()
	{
		//GD.Print(picked);
		if (_main.BMouse_left_down)
		{
			if (_main.BMousePicked && BPicked && BCan_move)
			{
				Position += new Vector2(1, 1);
			}
		}
		else
		{
			if (BHas_frame)
				Frame = 1;
			Bleep();
		}

	}
	private void OnMouseExited()
	{
		Position = Pos;
		if (!_main.BMouse_left_down && BHas_frame)
		{
			Frame = 0;
		}
	}

	private void NotPicked()
	{
		BPicked = false;
	}
	private void OnInputEvent(Node viewport, InputEvent inputEvent, int shapeIdx)
	{
		
		if (inputEvent.IsAction("mouse_left"))
		{
			//GD.Print("input"+picked);
			if (_main.BMouse_left_down)
			{
				GD.Print("BMouse_left_down");
				BPicked = true;
				_main.BMousePicked = true;
				Tap();
				if (BCan_move)
				{
					Position += new Vector2(1, 1);
				}
			}
			else
			{

				if (!BPicked)
				{
					GD.Print("bleep");
					if (BHas_frame)
						Frame = 1;
					Bleep();
				}
				else
				{
					GD.Print("GetClicked");
					GetClicked();
					Position = Pos;
				}
			}
		}
	}

	public virtual void GetClicked()
	{
		GD.Print(this.Name);
	}

	public virtual void Bleep()
	{
		BleepSound.Play();
	}

	public virtual void Tap()
	{
		TapSound.Play();
	}
}
