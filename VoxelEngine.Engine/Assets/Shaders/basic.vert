#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoord;
layout (location = 2) in float aTileLayer;
layout (location = 3) in float aAO;
layout (location = 4) in float aFaceLight;
layout (location = 5) in float aCutout;

out vec2 TexCoord;
flat out float TileLayer;
out float AO;
flat out float FaceLight;
flat out float Cutout;
out float FragDistance;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
    vec4 worldPos    = model * vec4(aPosition, 1.0);
    vec4 viewPos     = view * worldPos;
    FragDistance     = abs(viewPos.z);
    gl_Position      = projection * viewPos;
    TexCoord  = aTexCoord;
    TileLayer = aTileLayer;
    AO        = aAO;
    FaceLight = aFaceLight;
    Cutout    = aCutout;
}
