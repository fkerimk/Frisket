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
    vec4 original = sample_wrap(uv);
    vec4 oppositeX = sample_wrap(vec2(uv.x - 0.5, uv.y));
    vec4 oppositeY = sample_wrap(vec2(uv.x, uv.y - 0.5));
    vec4 oppositeXY = sample_wrap(vec2(uv.x - 0.5, uv.y - 0.5));

    float maskX = uSeamlessX > 0.001 ? band_mask(fract(uv.x - 0.5), max(uTexel.x * 2.0, uSeamWidth * 0.5)) * uSeamlessX : 0.0;
    float maskY = uSeamlessY > 0.001 ? band_mask(fract(uv.y - 0.5), max(uTexel.y * 2.0, uSeamWidth * 0.5)) * uSeamlessY : 0.0;

    float wOriginal = (1.0 - maskX) * (1.0 - maskY);
    float wX = maskX * (1.0 - maskY);
    float wY = (1.0 - maskX) * maskY;
    float wXY = maskX * maskY;

    fc = original * wOriginal
       + oppositeX * wX
       + oppositeY * wY
       + oppositeXY * wXY;
}
