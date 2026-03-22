#version 330 core
layout (location = 0) in vec2 aPosition;    // Quad Vertices (-0.5 bis 0.5)
layout (location = 1) in vec3 aStarPos;     // Instanz: Position auf Himmelskugel
layout (location = 2) in float aStarSize;   // Instanz: Größe
layout (location = 3) in float aStarPhase;  // Instanz: Twinkle-Phase

out float StarPhase;

uniform mat4 view;
uniform mat4 projection;
uniform float uTime;

void main()
{
    // Billboard: Quad immer zur Kamera gewandt
    // Extrahiere rechts/oben Vektoren aus View-Matrix
    vec3 right = vec3(view[0][0], view[1][0], view[2][0]);
    vec3 up    = vec3(view[0][1], view[1][1], view[2][1]);

    vec3 worldPos = aStarPos
        + right * aPosition.x * aStarSize
        + up    * aPosition.y * aStarSize;

    StarPhase = aStarPhase;
    vec4 pos = projection * view * vec4(worldPos, 1.0);
    gl_Position = pos.xyww;  // Far Plane
}
