shader_type canvas_item;
uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear;

uniform float tolerance: hint_range(0.0, 1.0, 0.01) = 0.1;
uniform vec4 fg : source_color;
uniform vec4 bg : source_color;

void fragment() {
	vec4 screen = texture(screen_texture, SCREEN_UV);
	float color_distance = distance(screen, fg);

	float prevAlpha = COLOR.a;
	if (color_distance <= tolerance){
		COLOR = mix(bg, fg, color_distance);
	} else {
		COLOR = vec4(0,0,0,1);
	}

	COLOR.a = prevAlpha;
}
