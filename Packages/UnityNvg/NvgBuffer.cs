
using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Unity.Collections;
using UnityEngine;

namespace NvgNET.Rendering.UnityNvg
{
	public class NvgBuffer : IDisposable
	{
		public readonly ComputeBuffer Buffer;
		bool _disposed;

		int _size;

		NvgBuffer(ComputeBuffer buffer)
		{
			Buffer = buffer;
		}


		public void Dispose()
		{
			if (_disposed)
				return;
			
			Buffer.Dispose();
			_disposed = true;
		}

		public static void UpdateB<T>([CanBeNull] ref NvgBuffer buffer, ReadOnlySpan<T> data) where T : struct
		{
			if (buffer == null || buffer._size < data.Length)
			{
				buffer?.Dispose();
				buffer = Create<T>(data);
			}
			else
			{
				NativeArray<T> d = buffer.Buffer.BeginWrite<T>(0, data.Length);
				data.CopyTo(d);
				buffer.Buffer.EndWrite<T>(d.Length);
			}
		}

		static NvgBuffer Create<T>(ReadOnlySpan<T> data) where T : struct
		{
			int allocationSize = Mathf.NextPowerOfTwo(data.Length);
			ComputeBuffer computeBuffer = new ComputeBuffer(allocationSize, Marshal.SizeOf(typeof(T)), ComputeBufferType.Structured, ComputeBufferMode.SubUpdates );
			
			NativeArray<T> d = computeBuffer.BeginWrite<T>(0, data.Length);
			data.CopyTo(d);
			computeBuffer.EndWrite<T>(d.Length);
			
			NvgBuffer buf = new NvgBuffer(computeBuffer)
			{
				_size = allocationSize
			};
			
			return buf;
		}
	}
}