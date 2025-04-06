using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace MonoGameProject.Terrain
{
    /// <summary>
    /// Custom vertex structure that includes position, color, normal, and texture coordinates
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPositionColorNormalTexture : IVertexType
    {
        public Vector3 Position;
        public Color Color;
        public Vector3 Normal;
        public Vector2 TextureCoordinate;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
            new VertexElement(16, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
            new VertexElement(28, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
        );

        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

        public VertexPositionColorNormalTexture(Vector3 position, Color color, Vector3 normal, Vector2 textureCoordinate)
        {
            Position = position;
            Color = color;
            Normal = normal;
            TextureCoordinate = textureCoordinate;
        }
    }
}
