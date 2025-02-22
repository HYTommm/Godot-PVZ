using Godot;
using System;

public partial class Button : Sprite2D
{
	Vector2 pos = new Vector2();
	public bool can_move = true;
	public bool picked = false;
	public AudioStreamPlayer BleepSound = new AudioStreamPlayer();
	public AudioStreamPlayer TapSound = new AudioStreamPlayer();
	[Export]
	MainNode2D main = new MainNode2D();
	public override void _Ready()
	{
		pos = Position;
		

		BleepSound.Stream = (AudioStream)GD.Load("res://sounds/bleep.ogg");
		TapSound.Stream = (AudioStream)GD.Load("res://sounds/tap.ogg");
		BleepSound.VolumeDb = -5;
		TapSound.VolumeDb = -6;

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
			Frame = 1;
			Bleep();
		}

	}
	private void OnMouseExited()
	{
		Position = pos;
		if (!main.mouse_left_down)
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
					Frame = 1;
					Bleep();
				}
				else
				{
					
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
