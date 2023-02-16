using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelSearch2;

/// <summary>
/// Common pixel implementation:
/// RGBA color packed in 32 bit unsigned integer with R in the least significant octet:
/// <code>
/// |-------|-------|-------|-------
/// A       B       G       R
/// </code>
/// </summary>
public readonly struct Pixel : IPixel<Pixel> {

    public byte R => (byte)PackedValue;

    public byte G => (byte)(PackedValue >> 8);

    public byte B => (byte)(PackedValue >> 16);

    public byte A => (byte)(PackedValue >> 24);

    public readonly uint PackedValue;

    public bool Equals(Pixel other) {
        return PackedValue == other.PackedValue;
    }
}

/// <summary>
/// Interface that allows user-defined RGBA providers
/// </summary>
public interface IPixel<T> where T : unmanaged {

    /// <summary>
    /// Red channel of the color
    /// </summary>
    byte R { get; }

    /// <summary>
    /// Green channel of the color
    /// </summary>
    byte G { get; }

    /// <summary>
    /// Blue channel of the color
    /// </summary>
    byte B { get; }

    /// <summary>
    /// Alpha channel of the color
    /// </summary>
    byte A { get; }

    bool Equals(T other);
}