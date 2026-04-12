shader_type canvas_item;

uniform float max_cd_time;
uniform float left_cd_time /*: hint_range(0.0, 10.0, 0.1)*/;

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	// Called for every pixel the material is visible on.

	vec2 uv = UV;
	vec4 color = COLOR;
	if (uv.y < left_cd_time/max_cd_time)
	{
		color.a = 0.5;
	}
	COLOR = color;
}

//void light() {
//	// Called for every pixel for every light affecting the CanvasItem.
//	// Uncomment to replace the default light processing function with this one.
//}
