#version 330 core
in vec3 TexCoord;
out vec4 FragColor;

uniform vec3 uZenithColor;
uniform vec3 uHorizonColor;
uniform vec3 uGroundColor;
uniform float uHorizonSharpness;

void main()
{
    float horizon = normalize(TexCoord).y;

    vec3 color;
    if (horizon >= 0.0)
    {
        float t = pow(horizon, uHorizonSharpness);
        color = mix(uHorizonColor, uZenithColor, t);
    }
    else
    {
        float t = pow(-horizon, uHorizonSharpness);
        color = mix(uHorizonColor, uGroundColor, t);
    }

    FragColor = vec4(color, 1.0);
}
