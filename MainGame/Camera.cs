using Godot;
using System;

public partial class Camera : Camera2D
{
	[Signal]
	public delegate void MoveEndEventHandler();

	public Vector2 StartMovePos = Vector2.Zero;
	public Vector2 EndMovePos = Vector2.Zero;

	public double time = 0;
	public bool isMoving = false;
	public double x = 0;
	public double a, xh, yh;
	public double halfa;
	public double FPS = 60;

	public override void _Ready()
	{

	}

	public override void _Process(double delta)
	{
		if (isMoving)
		{
			x += delta * FPS;
			double tempPosX = (-(xh / (halfa*halfa)) * (x - halfa - 0.2) * (x - halfa - 0.2) + xh)*delta * FPS;
			//double tempPosX = xh*delta * FPS;
			Vector2 tempPos = new((float)tempPosX, 0);
			Position += tempPos;
			if (Mathf.Abs(x - time * FPS) <= 1)
			{
				isMoving = false;
				Position = EndMovePos;
				EmitSignal(SignalName.MoveEnd);
			}
		}
		
	}

	public void Move(Vector2 pos, double time)
	{
		
		isMoving = true;
		StartMovePos = Position;
		EndMovePos = pos;
		this.time = time;
		a = time * FPS;
		halfa = time * FPS / 2;
		x = 0;
		xh = (EndMovePos.X - StartMovePos.X) / a*3/2;
		//xh = (EndMovePos.X - StartMovePos.X) / a;
		yh = Mathf.Abs(StartMovePos.Y - EndMovePos.Y) / a*3/2;

	}
}
