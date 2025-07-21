using Godot;
using System;
using static ResourceManager.Sounds;

public partial class GameBaseButton : Sprite2D
{
	public Vector2 pos = new Vector2();
	public bool can_move = true;
	public bool picked = false;
	public bool has_frame = true;
	public AudioStreamPlayer BleepSound = new AudioStreamPlayer();
	public AudioStreamPlayer TapSound = new AudioStreamPlayer();
	[Export]
	MainNode2D main;
	public override void _Ready()
	{
		pos = Position;
		

		BleepSound.Stream = Sound_Bleep;
		TapSound.Stream = Sound_Tap;

		AddChild(BleepSound);
		AddChild(TapSound);
	}

	private void OnMouseEntered()
	{
		//GD.Print(picked);
		if (main.mouse_left_down)
		{
			if (main.mouse_picked && picked && can_move)
			{
				Position += new Vector2(1, 1);
			}
		}
		else
		{
			if (has_frame)
				Frame = 1;
			Bleep();
		}

	}
	private void OnMouseExited()
	{
		Position = pos;
		if (!main.mouse_left_down && has_frame)
		{
			Frame = 0;
		}
	}

	private void NotPicked()
	{
		picked = false;
	}
	private void OnInputEvent(Node viewport, InputEvent inputevent, int shape_idx)
	{
		
		if (inputevent.IsAction("mouse_left"))
		{
			//GD.Print("input"+picked);
			if (main.mouse_left_down)
			{
				GD.Print("mouse_left_down");
				picked = true;
				main.mouse_picked = true;
				Tap();
				if (can_move)
				{
					Position += new Vector2(1, 1);
				}
			}
			else
			{

				if (!picked)
				{
					GD.Print("bleep");
					if (has_frame)
						Frame = 1;
					Bleep();
				}
				else
				{
					GD.Print("GetClicked");
					GetClicked();
					Position = pos;
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
