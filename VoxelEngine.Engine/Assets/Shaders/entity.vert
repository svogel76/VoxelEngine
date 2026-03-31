#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoord;
layout (location = 2) in vec4 aColor;
layout (location = 3) in float aShade;
layout (location = 4) in mat4 aInstanceModel;

out vec2 TexCoord;
out vec4 TintColor;
out float Shade;
out float FragDistance;

uniform mat4 view;
uniform mat4 projection;

void main()
{
    vec4 worldPos = aInstanceModel * vec4(aPosition, 1.0);
    vec4 viewPos = view * worldPos;
    FragDistance = abs(viewPos.z);
    gl_Position = projection * viewPos;
    TexCoord = aTexCoord;
    TintColor = aColor;
    Shade = aShade;
}
