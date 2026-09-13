/// <summary>
/// 关卡的场景类型。
///
/// 这里只表达"这一关用哪一种场景"，草坪尺寸、单位大小、原点坐标、背景贴图、BGM
/// 这些细节仍然由 Scene 的子类自己管理（见 MainGame/Common/Scene.cs），
/// 不进入关卡数据。
/// </summary>
public enum SceneKind
{
	/// <summary> 白天草坪 </summary>
	Day,

	/// <summary> 白天泳池 </summary>
	Pool,
}
