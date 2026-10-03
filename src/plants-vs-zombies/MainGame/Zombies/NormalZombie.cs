using Godot;

public partial class NormalZombie : TieZombie
{
    public NormalZombie()
    {
        //HP = 270;
        //MaxHP = 270;
    }

    public override void Init()
    {
        Log.Debug("NormalZombie Init called");
    }
}