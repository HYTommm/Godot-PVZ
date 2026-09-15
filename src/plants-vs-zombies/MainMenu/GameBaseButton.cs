using Godot;
using System;
using static ResourceDB.Sounds;

public partial class GameBaseButton : Sprite2D
{
	public Vector2 Pos = new();
	public bool BCan_move = true;
	public bool BPicked = false;
	public bool BMouseEntered = false;
	//public bool BMouse_left_down = false;
	//public bool BMouse_left_up = false;
	public bool BHas_frame = true;
	public AudioStreamPlayer BleepSound = new();
	public AudioStreamPlayer TapSound = new();
	[Export] protected MainNode2D Main;
	public override void _Ready()
	{
		Pos = Position;

		// 走信号，不自己重写 _Input。鼠标状态是在 Main 的 _Input 里更新的，
		// 这边再写一份的话响应时机取决于节点树顺序，可能读到还没更新的值
		if (Main != null)
		{
			Main.MouseLeftDown += MouseLeftDown;
			Main.MouseLeftUp += MouseLeftUp;
		}

		BleepSound.Stream = Sound_Bleep;
		TapSound.Stream = Sound_Tap;

		AddChild(BleepSound);
		AddChild(TapSound);
	}

	public override void _ExitTree()
	{
		if (Main != null)
		{
			Main.MouseLeftDown -= MouseLeftDown;
			Main.MouseLeftUp -= MouseLeftUp;
		}
	}

	public void MouseLeftUp()
	{
		GD.Print("MouseLeftUp");
		if (BPicked)
		{
			GD.Print("MouseLeftUp : BPicked");
			Main.BMousePicked = false;
			Position = Pos;
			if (!Main.BMouse_left_down && BHas_frame)
			{
				Frame = 0;
			}
			NotPicked();
			if (BMouseEntered)
			{
				GetClicked();
			}
		}
		else if (BMouseEntered)
		{
			GD.Print("MouseLeftUp : bleep");
			if (BHas_frame)
				Frame = 1;
			Bleep();
		}
	}

	public void MouseLeftDown()
	{
		GD.Print("BMouse_left_down");
		if (BMouseEntered)
		{
			BPicked = true;
			Main.BMousePicked = true;
			Tap();
			if (BCan_move)
			{
				Position += new Vector2(1, 1);
			} 
		}
	}

	private void OnMouseEntered()
	{
		//GD.Print(picked);
		GD.Print("MouseEntered");
		BMouseEntered = true;
		if (Main.BMouse_left_down)
		{
			if (Main.BMousePicked && BPicked && BCan_move)
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
		GD.Print("MouseExited");
		BMouseEntered = false;
		Position = Pos;
		if (!Main.BMouse_left_down && BHas_frame)
		{
			Frame = 0;
		}
	}

	private void NotPicked()
	{
		GD.Print("NotPicked");
		BPicked = false;
	}

	private void OnInputEvent(Node viewport, InputEvent inputEvent, int shapeIdx)
	{
		
		//if (inputEvent.IsAction("mouse_left"))
		//{
		//	//GD.Print("input"+picked);
		//	if (Main.BMouse_left_down)
		//	{
		//		GD.Print("BMouse_left_down");
		//		BPicked = true;
		//		Main.BMousePicked = true;
		//		Tap();
		//		if (BCan_move)
		//		{
		//			Position += new Vector2(1, 1);
		//		}
		//	}
		//	else
		//	{

		//		if (!BPicked)
		//		{
		//			GD.Print("bleep");
		//			if (BHas_frame)
		//				Frame = 1;
		//			Bleep();
		//		}
		//		else
		//		{
		//			GD.Print("GetClicked");
		//			GetClicked();
		//			Position = Pos;
		//		}
		//	}
		//}
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
