using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelSearch2;

/// <summary>
/// Wraps an array of pixels in an image-like layout
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly ref struct PixelArray<T> where T : unmanaged, IPixel<T> {

    /// <summary>
    /// Total size of the pixel array
    /// </summary>
    public int Size => Width * Height;

    public readonly ReadOnlySpan<T> Pixels;
    public readonly int Width;
    public readonly int Height;

    /// <summary>
    /// Creates a new image pixel array with a span, width and height. The span and dimensions must be equal.
    /// </summary>
    /// <param name="pixels">Span of the pixels</param>
    /// <param name="width">Width of the image</param>
    /// <param name="height">Height of the image</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public PixelArray(ReadOnlySpan<T> pixels, int width, int height) {
        if (pixels.Length != width*height) {
            throw new ArgumentOutOfRangeException(nameof(pixels), "Pixel span does not match width and height.");
        }
        Pixels = pixels;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// <inheritdoc cref="PixelArray{T}.PixelArray(T*, int, int)" />
    /// </summary>
    public unsafe PixelArray(IntPtr data, int width, int height) : this((T*)data, width, height) {
    }

    /// <summary>
    /// Creates a new pixel array from a raw pointer.
    /// </summary>
    /// <param name="data">Pointer to the pixel data</param>
    /// <param name="width">Width of the image</param>
    /// <param name="height">Height of the image</param>
    public unsafe PixelArray(T* data, int width, int height) {
        Pixels = new ReadOnlySpan<T>(data, width * height);
        Width = width;
        Height = height;
    }
}