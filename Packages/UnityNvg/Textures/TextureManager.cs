using System;

namespace NvgNET.Rendering.OpenGL.Textures
{
    public sealed class TextureManager : IDisposable
    {
        private NvgTexture[] _textures;
        private int _count;

        private NvgTexture _default = default;
        private readonly bool _destroyImmediate;

        public TextureManager(bool destroyImmediate)
        {
            _destroyImmediate = destroyImmediate;
            _textures = Array.Empty<NvgTexture>();
            _count = 0;
        }

        public ref NvgTexture AllocTexture()
        {
            int tex = -1;

            for (int i = 0; i < _count; i++)
            {
                if (_textures[i].Id == 0)
                {
                    tex = i;
                }
            }

            if (tex == -1)
            {
                if (_count + 1 > _textures.Length)
                {
                    int ctextures = Math.Max(_count + 1, 4) + _textures.Length / 2;
                    Array.Resize(ref _textures, ctextures);
                }
                tex = _count++;
            }

            _textures[tex] = new NvgTexture();

            return ref _textures[tex];
        }

        public ref NvgTexture FindTexture(int id)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_textures[i].Id == id)
                {
                    return ref _textures[i];
                }
            }
            return ref _default;
        }

        public bool DeleteTexture(int id)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_textures[i].Id == id)
                {
                    _textures[i].Dispose();
                    _textures[i] = default;
                    return true;
                }
            }
            return false;
        }

        public void Dispose()
        {
            for (int i = 0; i < _count; i++)
            {
                if (_destroyImmediate)
                    UnityEngine.Object.DestroyImmediate(_textures[i].Texture);
                else
                    UnityEngine.Object.Destroy(_textures[i].Texture);
                _textures[i].Dispose();
            }
        }

    }
}
