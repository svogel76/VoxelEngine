#version 330 core
in vec2 TexCoord;
out vec4 FragColor;

uniform sampler2D uTexture;
uniform vec3  uColor;
uniform float uOpacity;

void main()
{
    vec4 texColor = texture(uTexture, TexCoord);
    FragColor = vec4(uColor * texColor.rgb, texColor.a * uOpacity);
}
