using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace VoxelEngine.Rendering;

public class Shader : IDisposable
{
    private readonly GL   _gl;
    private readonly uint _handle;

    public Shader(GL gl, string vertexPath, string fragmentPath)
    {
        _gl = gl;

        uint vert = CompileShader(ShaderType.VertexShader,   File.ReadAllText(vertexPath));
        uint frag = CompileShader(ShaderType.FragmentShader, File.ReadAllText(fragmentPath));

        _handle = gl.CreateProgram();
        gl.AttachShader(_handle, vert);
        gl.AttachShader(_handle, frag);
        gl.LinkProgram(_handle);

        gl.GetProgram(_handle, GLEnum.LinkStatus, out int linkStatus);
        if (linkStatus == 0)
            throw new Exception($"Shader link error: {gl.GetProgramInfoLog(_handle)}");

        gl.DeleteShader(vert);
        gl.DeleteShader(frag);
    }

    private uint CompileShader(ShaderType type, string source)
    {
        uint shader = _gl.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);

        _gl.GetShader(shader, GLEnum.CompileStatus, out int status);
        if (status == 0)
            throw new Exception($"Shader compile error ({type}): {_gl.GetShaderInfoLog(shader)}");

        return shader;
    }

    public void Use() => _gl.UseProgram(_handle);

    public unsafe void SetMatrix4(string name, Matrix4X4<float> matrix)
    {
        int location = _gl.GetUniformLocation(_handle, name);
        var matrixArray = new float[]
        {
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44
        };
        fixed (float* ptr = matrixArray)
            _gl.UniformMatrix4(location, 1, false, ptr);
    }

    public void SetInt(string name, int value)
    {
        int location = _gl.GetUniformLocation(_handle, name);
        _gl.Uniform1(location, value);
    }

    public void Dispose() => _gl.DeleteProgram(_handle);
}
