#version 330
in vec2 fragTexCoord;
uniform sampler2D texture0;
uniform vec2 uTexel;
out vec4 fc;

float mitchell1d(float x) {
    float ax = abs(x);
    float ax2 = ax * ax;
    float ax3 = ax2 * ax;
    const float B = 1.0 / 3.0;
    const float C = 1.0 / 3.0;
    if (ax < 1.0) return ((12.0 - 9.0 * B - 6.0 * C) * ax3 + (-18.0 + 12.0 * B + 6.0 * C) * ax2 + (6.0 - 2.0 * B)) / 6.0;
    if (ax < 2.0) return ((-B - 6.0 * C) * ax3 + (6.0 * B + 30.0 * C) * ax2 + (-12.0 * B - 48.0 * C) * ax + (8.0 * B + 24.0 * C)) / 6.0;
    return 0.0;
}

void main() {
    vec2 p = fragTexCoord / uTexel;
    vec2 f = fract(p);
    ivec2 b = ivec2(floor(p));

    vec4 color = vec4(0.0);
    float total = 0.0;

    for (int y = -1; y <= 2; y++) {
        for (int x = -1; x <= 2; x++) {
            vec2 o = vec2(float(x), float(y));
            vec2 suv = clamp((vec2(b) + o + 0.5) * uTexel, 0.001, 0.999);
            vec2 d = o + 0.5 - f;
            float w = mitchell1d(d.x) * mitchell1d(d.y);
            color += texture(texture0, suv) * w;
            total += w;
        }
    }

    fc = color / max(total, 1e-8);
}
