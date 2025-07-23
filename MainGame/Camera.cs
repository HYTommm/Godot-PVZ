using Godot;
using System;

public partial class Camera : Camera2D
{
	[Signal]
	public delegate void MoveEndEventHandler();

	public Vector2 StartMovePos = Vector2.Zero;
	public Vector2 EndMovePos = Vector2.Zero;

	public double Time = 0;
	public bool BIsMoving = false;
	public double X = 0;
	public double A, Xh, Yh;
	public double Halfa;
	public double FPS = 60;

	public override void _Ready()
	{

	}

	public override void _Process(double delta)
	{
		if (BIsMoving)
		{
			X += delta * FPS;
			double tempPosX = (-(Xh / (Halfa*Halfa)) * (X - Halfa - 0.2) * (X - Halfa - 0.2) + Xh) * delta * FPS;
			//double tempPosX = xh*delta * FPS;
			Vector2 tempPos = new((float)tempPosX, 0);
			Position += tempPos;
			if (Mathf.Abs(X - Time * FPS) <= 1)
			{
				BIsMoving = false;
				Position = EndMovePos;
				EmitSignal(SignalName.MoveEnd);
			}
		}
		
	}

	public void Move(Vector2 pos, double time)
	{
		
		BIsMoving = true;
		StartMovePos = Position;
		EndMovePos = pos;
		Time = time;
		A = time * FPS;
		Halfa = time * FPS / 2;
		X = 0;
		Xh = (EndMovePos.X - StartMovePos.X) / A*3/2;
		//xh = (EndMovePos.X - StartMovePos.X) / a;
		Yh = Mathf.Abs(StartMovePos.Y - EndMovePos.Y) / A*3/2;

	}
}
