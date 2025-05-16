shader_type canvas_item;
uniform sampler2D screen_texture : hint_screen_texture, 
repeat_disable, filter_linear;

uniform float tolerance: hint_range(0.0, 1.0, 0.01) = 0.1;
uniform vec4 a1 : source_color;
uniform vec3 a2 : source_color;

uniform vec3 b1 : source_color;
uniform vec3 b2 : source_color;

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	vec4 c = texture(screen_texture, SCREEN_UV);
	float color_distance = distance(c, a1);
	
	
		COLOR = mix(vec4(1,1,1,1), c, color_distance);
		COLOR.a = c.a;
	
}

//void light() {
//	// Called for every pixel for every light affecting the CanvasItem.
//	// Uncomment to replace the default light processing function with this one.
//}
