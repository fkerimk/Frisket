#version 330
in vec2 fragTexCoord;
uniform sampler2D texture0;
uniform vec3 uH0, uH1, uH2;
uniform float uBright, uContrast, uSat, uSharp, uGamma;
uniform vec2 uTexel;
uniform float uColorBits;
uniform float uNoise;
uniform float uBleed;
uniform float uDither;
uniform float uJitter;
uniform float uChromaShift;
out vec4 fc;
vec4 t(vec2 u) { return (u.x<0.||u.x>1.||u.y<0.||u.y>1.) ? vec4(0.) : texture(texture0,u); }
float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453123); }
float bayer4(vec2 p) {
    int x = int(mod(p.x, 4.0));
    int y = int(mod(p.y, 4.0));
    int i = x + y * 4;
    float m[16] = float[](
        0.0, 8.0, 2.0, 10.0,
        12.0, 4.0, 14.0, 6.0,
        3.0, 11.0, 1.0, 9.0,
        15.0, 7.0, 13.0, 5.0
    );
    return (m[i] + 0.5) / 16.0;
}
void main() {
    vec3 s = mat3(uH0,uH1,uH2) * vec3(fragTexCoord,1.);
    vec2 uv = s.xy/s.z;
    if(uJitter > 0.001) {
        float row = floor(fragTexCoord.y / uTexel.y);
        uv.x += (hash(vec2(row, 17.0)) - 0.5) * uTexel.x * uJitter * 4.0;
    }
    vec4 c = t(uv);
    if(c.a==0.){fc=vec4(0.);return;}
    if(uSharp>0.001){
        vec4 b=(t(uv+vec2(uTexel.x,0))+t(uv-vec2(uTexel.x,0))+t(uv+vec2(0,uTexel.y))+t(uv-vec2(0,uTexel.y)))*.25;
        c.rgb=mix(c.rgb,c.rgb+(c.rgb-b.rgb)*2.,uSharp);
    }
    if(uBleed > 0.001) {
        vec3 left = t(uv - vec2(uTexel.x, 0)).rgb;
        vec3 right = t(uv + vec2(uTexel.x, 0)).rgb;
        vec3 bleed = vec3(left.r, c.g, right.b);
        c.rgb = mix(c.rgb, bleed, uBleed * 0.35);
    }
    if(uChromaShift > 0.001) {
        vec2 shift = vec2(uTexel.x * uChromaShift, uTexel.y * uChromaShift * 0.5);
        float r = t(uv - shift).r;
        float b = t(uv + shift).b;
        c.rgb = vec3(r, c.g, b);
    }
    c.rgb = (c.rgb+uBright-.5)*uContrast+.5;
    float l=dot(c.rgb,vec3(.2126,.7152,.0722));
    c.rgb = pow(clamp(mix(vec3(l),c.rgb,uSat),.001,1.),vec3(1./uGamma));
    if(uColorBits < 7.5) {
        float levels = pow(2.0, uColorBits) - 1.0;
        float threshold = (uDither > 0.001 ? bayer4(floor(fragTexCoord / uTexel)) - 0.5 : 0.0) * uDither;
        c.rgb = floor(clamp(c.rgb + threshold / max(levels, 1.0), 0.0, 1.0) * levels + 0.5) / levels;
    }
    if(uNoise > 0.001) {
        float grain = (hash(floor(fragTexCoord / uTexel)) - 0.5) * uNoise;
        c.rgb = clamp(c.rgb + grain, 0.0, 1.0);
    }
    fc=vec4(c.rgb,c.a);
}
