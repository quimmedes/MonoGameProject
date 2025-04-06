using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.InteropServices;

namespace MonoGameProject.Terrain
{
    /// <summary>
    /// Custom vertex structure that includes position, normal, color, and texture coordinates
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPositionNormalColorTexture : IVertexType
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Color Color;
        public Vector2 TextureCoordinate;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
            new VertexElement(24, VertexElementFormat.Color, VertexElementUsage.Color, 0),
            new VertexElement(28, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
        );

        public VertexPositionNormalColorTexture(Vector3 position, Vector3 normal, Color color, Vector2 textureCoordinate)
        {
            Position = position;
            Normal = normal;
            Color = color;
            TextureCoordinate = textureCoordinate;
        }

        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
    }
}
