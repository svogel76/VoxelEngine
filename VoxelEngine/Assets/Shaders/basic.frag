#version 330 core
in vec2 TexCoord;
flat in float TileLayer;
in float AO;
flat in float FaceLight;
out vec4 FragColor;

uniform sampler2DArray uTexture;
uniform float uGlobalLight;
uniform vec3  uSunColor;

void main()
{
    float ao       = AO / 3.0;
    float aoFactor = mix(0.3, 1.0, ao);
    float light    = FaceLight * uGlobalLight;
    light          = max(light, 0.03);
    vec4 texColor  = texture(uTexture, vec3(TexCoord, TileLayer));
    FragColor      = vec4(texColor.rgb * uSunColor * light * aoFactor, texColor.a);
}
