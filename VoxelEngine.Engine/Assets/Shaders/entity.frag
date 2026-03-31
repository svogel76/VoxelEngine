#version 330 core
in vec2 TexCoord;
in vec4 TintColor;
in float Shade;
in float FragDistance;

out vec4 FragColor;

uniform sampler2D uTexture;
uniform float uGlobalLight;
uniform vec3 uSunColor;
uniform vec3 uFogColor;
uniform float uFogStart;
uniform float uFogEnd;

void main()
{
    vec4 texColor = texture(uTexture, TexCoord);
    if (texColor.a < 0.1)
        discard;

    float light = max(Shade * uGlobalLight, 0.05);
    vec4 litColor = vec4(texColor.rgb * TintColor.rgb * uSunColor * light, texColor.a * TintColor.a);
    float fogFactor = 1.0 - clamp((uFogEnd - FragDistance) / (uFogEnd - uFogStart), 0.0, 1.0);
    vec3 finalColor = mix(litColor.rgb, uFogColor, fogFactor);
    FragColor = vec4(finalColor, litColor.a);
}
