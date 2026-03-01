using NvgNET.Images;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NvgNET.Rendering.OpenGL.Textures
{
    public struct NvgTexture : IDisposable
    {
        private static int _idCounter = 0;

        public Texture2D Texture => _textureID;
        private Texture2D _textureID;
        private ImageFlags _flags;
        public ImageFlags Flags => _flags;

        public int Id { get; private set; }

        public Size Size { get; private set; }

        public Texture TextureType { get; private set; }

        public void Load(Size size, ImageFlags imageFlags, Rendering.Texture type, ReadOnlySpan<byte> data)
        {
            Id = ++_idCounter;
            _textureID = new Texture2D(size.Width, size.Height, type == Rendering.Texture.Rgba ? TextureFormat.RGBA32 : TextureFormat.R8, (imageFlags & ImageFlags.GenerateMimpas) == ImageFlags.GenerateMimpas);
            _textureID.filterMode = (imageFlags & ImageFlags.Nearest) == ImageFlags.Nearest ? FilterMode.Point : FilterMode.Bilinear;
            _textureID.wrapModeU = (imageFlags & ImageFlags.RepeatX) == ImageFlags.RepeatX ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            _textureID.wrapModeV = (imageFlags & ImageFlags.RepeatY) == ImageFlags.RepeatY ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            
            Size = size;
            TextureType = type;
            _flags = imageFlags;

            Load(data);
        }

        private void Load(ReadOnlySpan<byte> data)
        {
            data.CopyTo(_textureID.GetPixelData<byte>(0));
            _textureID.Apply();
        }

        public void Update(Rectangle bounds, ReadOnlySpan<byte> data)
        {
            if (TextureType == Rendering.Texture.Rgba)
            {
                Span2D<Color32> textureData = _textureID.GetPixelData<Color32>(0).AsSpan().AsSpan2D(Size.Height, Size.Width);
                Span2D<Color32> textureDataSlice = textureData.Slice(bounds.Y, bounds.X, bounds.Height, bounds.Width);
                MemoryMarshal.Cast<byte, Color32>(data).AsSpan2D(Size.Height, Size.Width).Slice(bounds.Y, bounds.X, bounds.Height, bounds.Width).CopyTo(textureDataSlice);
            }
            else
            {
                Span2D<byte> textureData = _textureID.GetPixelData<byte>(0).AsSpan().AsSpan2D(Size.Height, Size.Width);
                Span2D<byte> textureDataSlice = textureData.Slice(bounds.Y, bounds.X, bounds.Height, bounds.Width);
                data.AsSpan2D(Size.Height, Size.Width).Slice(bounds.Y, bounds.X, bounds.Height, bounds.Width).CopyTo(textureDataSlice);
            }

            _textureID.Apply();
        }

        public bool HasFlag(ImageFlags flag)
        {
            return _flags.HasFlag(flag);
        }

        public void Dispose()
        {
            _textureID = null;
        }

    }
}