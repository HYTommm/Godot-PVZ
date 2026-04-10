shader_type canvas_item;
render_mode unshaded;

uniform vec4 modulate_color : source_color = vec4(0.5, 0.5, 0.5, 0.5);
uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;
void vertex()
{
    // 无需修改顶点，保持默认即可
}

void fragment()
{
    vec4 color = textureLod(screen_texture, SCREEN_UV, 0.0);
    COLOR = color * modulate_color * 2.0;
}