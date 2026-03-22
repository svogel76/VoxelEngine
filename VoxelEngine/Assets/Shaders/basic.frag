#version 330 core
in vec2 TexCoord;
flat in float TileLayer;
in float AO;
out vec4 FragColor;

uniform sampler2DArray uTexture;

void main()
{
    float aoFactor = mix(0.3, 1.0, AO / 3.0);
    FragColor = texture(uTexture, vec3(TexCoord, TileLayer)) * vec4(vec3(aoFactor), 1.0);
}
