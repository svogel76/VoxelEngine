#version 330 core
in vec2 TexCoord;
flat in float TileLayer;
in float AO;
flat in float FaceLight;
in float FragDistance;
out vec4 FragColor;

uniform sampler2DArray uTexture;
uniform float uGlobalLight;
uniform vec3  uSunColor;
uniform vec3  uFogColor;
uniform float uFogStart;
uniform float uFogEnd;
uniform float uAlphaMultiplier;

void main()
{
    float ao       = AO / 3.0;
    float aoFactor = mix(0.3, 1.0, ao);
    float light    = FaceLight * uGlobalLight;
    light          = max(light, 0.03);
    vec4 texColor  = texture(uTexture, vec3(TexCoord, TileLayer));
    vec4 litColor  = vec4(texColor.rgb * uSunColor * light * aoFactor, texColor.a);

    float fogFactor = 1.0 - clamp(
        (uFogEnd - FragDistance) / (uFogEnd - uFogStart),
        0.0, 1.0);
    vec3 finalColor = mix(litColor.rgb, uFogColor, fogFactor);
    FragColor = vec4(finalColor, litColor.a * uAlphaMultiplier);
}
