#version 330 core
// Vertex-Attribute: position (screen-space px), texCoord (0..1), tileLayer
layout (location = 0) in vec2  aPosition;
layout (location = 1) in vec2  aTexCoord;
layout (location = 2) in float aTileLayer;

out vec2  vTexCoord;
flat out float vTileLayer;

// Orthographische Projektion: pixel → NDC
uniform mat4 uProjection;

void main()
{
    gl_Position = uProjection * vec4(aPosition, 0.0, 1.0);
    vTexCoord   = aTexCoord;
    vTileLayer  = aTileLayer;
}
