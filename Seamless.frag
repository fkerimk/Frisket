#version 330
in vec2 fragTexCoord;
uniform sampler2D texture0;
uniform vec2 uTexel;
uniform float uSeamlessX;
uniform float uSeamlessY;
uniform float uSeamWidth;
out vec4 fc;

vec4 sample_wrap(vec2 uv) {
    return texture(texture0, fract(uv));
}

float band_mask(float coord, float width) {
    float edge0 = 0.5 - width;
    float edge1 = 0.5;
    float edge2 = 0.5 + width;
    return smoothstep(edge0, edge1, coord) * (1.0 - smoothstep(edge1, edge2, coord));
}

void main() {
    vec2 uv = fragTexCoord;
    vec2 offset = vec2(uSeamlessX > 0.001 ? 0.5 : 0.0, uSeamlessY > 0.001 ? 0.5 : 0.0);
    vec2 seamUv = fract(uv - offset);
    vec4 original = sample_wrap(uv);
    vec4 opposite = sample_wrap(seamUv);

    float maskX = uSeamlessX > 0.001 ? band_mask(seamUv.x, max(uTexel.x * 2.0, uSeamWidth * 0.5)) * uSeamlessX : 0.0;
    float maskY = uSeamlessY > 0.001 ? band_mask(seamUv.y, max(uTexel.y * 2.0, uSeamWidth * 0.5)) * uSeamlessY : 0.0;
    float blend = max(maskX, maskY);

    fc = mix(original, opposite, blend);
}
