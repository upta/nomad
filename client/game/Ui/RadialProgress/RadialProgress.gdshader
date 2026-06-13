shader_type canvas_item;

uniform float outer_progress : hint_range(0.0, 1.0) = 1.0;
uniform float inner_progress : hint_range(0.0, 1.0) = 1.0;
uniform vec4 outer_color : source_color = vec4(1.0);
uniform vec4 inner_color : source_color = vec4(1.0, 1.0, 0.0, 1.0);
uniform vec4 background_color : source_color = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 fill_color : source_color = vec4(1.0, 1.0, 1.0, 0.3);
uniform vec4 fill_center_color : source_color = vec4(1.0, 1.0, 1.0, 0.1);
uniform float fill_alpha : hint_range(0.0, 1.0) = 0.0;
uniform float line_width : hint_range(1.0, 50.0) = 10.0;
uniform vec2 resolution = vec2(100.0, 100.0);
uniform float anti_alias_width : hint_range(0.5, 4.0) = 1.5;

float get_circle_progress(vec2 uv, float progress) {
    vec2 center = vec2(0.5, 0.5);
    vec2 pos = uv - center;

    // Distance from center
    float dist = length(pos);

    // Create circle with line width that scales with resolution
    float outer_radius = 0.48; // Use more of the available space
    float normalized_line_width = line_width / min(resolution.x, resolution.y);
    float inner_radius = max(0.0, outer_radius - normalized_line_width); // Ensure inner_radius doesn't go negative

    // Anti-aliasing width in UV space
    float aa_width = anti_alias_width / min(resolution.x, resolution.y);

    // Check if we're in the ring area with smooth transitions
    float outer_mask = 1.0 - smoothstep(outer_radius - aa_width * 0.5, outer_radius + aa_width * 0.5, dist);
    float inner_mask = smoothstep(inner_radius - aa_width * 0.5, inner_radius + aa_width * 0.5, dist);
    float ring_mask = outer_mask * inner_mask;

    if (ring_mask <= 0.0) {
        return 0.0;
    }

    // Convert to polar coordinates for progress calculation
    float angle = atan(pos.y, pos.x);
    // Normalize angle to 0-1 range, starting from top (12 o'clock)
    angle = (angle + PI) / (2.0 * PI);
    angle = fract(angle + 0.75); // Rotate so 0 is at top

    // Progress goes from 1.0 (full circle) to 0.0 (empty)
    // So we show the circle where angle is GREATER than (1.0 - progress)
    float progress_mask = step(1.0 - progress, angle);

    return ring_mask * progress_mask;
}

float get_circle_fill(vec2 uv) {
    vec2 center = vec2(0.5, 0.5);
    vec2 pos = uv - center;

    // Distance from center
    float dist = length(pos);

    // Create fill inside the inner circle
    float outer_radius = 0.48;
    float normalized_line_width = line_width / min(resolution.x, resolution.y);
    float inner_radius = max(0.0, outer_radius - normalized_line_width);

    // Anti-aliasing width in UV space
    float aa_width = anti_alias_width / min(resolution.x, resolution.y);

    // Smooth transition at the edge
    float mask = 1.0 - smoothstep(inner_radius - aa_width * 0.5, inner_radius + aa_width * 0.5, dist);

    if (mask <= 0.0) {
        return 0.0; // Outside the fill area
    }

    // Create gradient from center (0.0) to edge (1.0)
    return (dist / inner_radius) * mask;
}

void fragment() {
    vec2 uv = UV;

    float outer_mask = get_circle_progress(uv, outer_progress);
    float inner_mask = get_circle_progress(uv, inner_progress);
    float fill_gradient = get_circle_fill(uv);

    // Layer the colors: background -> fill -> inner -> outer
    vec4 color = background_color;

    // Add gradient fill with animated alpha
    if (fill_gradient > 0.0) {
        // Create gradient from center color to edge color
        vec4 gradient_color = mix(fill_center_color, fill_color, fill_gradient);
        gradient_color.a *= fill_alpha;
        color = mix(color, gradient_color, gradient_color.a);
    }

    // Add inner circle
    color = mix(color, inner_color, inner_mask);

    // Add outer circle (overwrites inner where present)
    color = mix(color, outer_color, outer_mask);

    COLOR = color;
}
