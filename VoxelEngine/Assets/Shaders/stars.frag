#version 330 core
in float StarPhase;
out vec4 FragColor;

uniform float uTime;
uniform float uOpacity;
uniform vec3  uStarColor;

void main()
{
    float twinkle = 0.75 + 0.25 * sin(uTime * 2.0 + StarPhase);
    FragColor = vec4(uStarColor * twinkle, uOpacity * twinkle);
}
