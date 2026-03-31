#version 330 core
layout (location = 0) in vec3 aPosition;

out vec3 TexCoord;

uniform mat4 projection;
uniform mat4 view;

void main()
{
    TexCoord = aPosition;
    vec4 pos = projection * view * vec4(aPosition, 1.0);
    // z = w damit Skybox immer auf Far Plane landet (maximale Tiefe)
    gl_Position = pos.xyww;
}
