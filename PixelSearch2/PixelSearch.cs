using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PixelSearch2;

/// <summary>
/// <para>Provides search options for the pixel search:</para>
/// <code>
/// PixelTolerance: 0 - 1 (%), individual pixel tolerance
/// ImageTolerance: 0 - 1 (%), whole image tolerance
/// </code>
/// Where <c>0</c> is none and <c>1</c> is max tolerance (100%)
/// 
/// </summary>
public readonly record struct SearchOptions {

    /// <summary>
    /// Default search options with tolerance at 0%
    /// </summary>
    public static SearchOptions Default => new SearchOptions(0, 0);

    public readonly float PixelTolerance;
    public readonly float ImageTolerance;

    /// <summary>
    /// Constructs a new set of search options, <c>null</c> for default values
    /// </summary>
    /// <param name="pixelTolerance">Pixel tolerance</param>
    /// <param name="imageTolerance">Image tolerance</param>
    public SearchOptions(float? pixelTolerance = null, float? imageTolerance = null) {
        PixelTolerance = Math.Clamp(pixelTolerance ?? 0, 0, 1);
        ImageTolerance = Math.Clamp(imageTolerance ?? 0, 0, 1);
    }
}

public class PixelSearch {

    /// <summary>
    /// Compares 2 arrays of pixels with <see cref="SearchOptions.Default"/>, accepts a <see cref="IPixel{T}"/> type argument to allow user-defined RGBA providers
    /// 
    /// <para>Any color with 0 alpha is considered a wildcard color and allowed to pass</para>
    /// <para>The arrays are required to be in row-major order for the method to work</para>
    /// </summary>
    /// <param name="input">The input pixel array</param>
    /// <param name="source">The source pixel array</param>
    /// <param name="clip">Clip rectangle that allows searching a slice of the source array</param>
    /// <param name="location">Output location if a match is found</param>
    /// <returns><c>true</c> if a match is found</returns>
    /// <exception cref="ArgumentOutOfRangeException">Throws if source, input or clip has invalid sizes</exception>
    public static bool FindPixels<T>(PixelArray<T> input, PixelArray<T> source, out (int x, int y) location) where T : unmanaged, IPixel<T> {
        return FindPixels(input, source, SearchOptions.Default, (0, 0, source.Width, source.Height), out location);
    }

    /// <summary>
    /// Compares 2 arrays of pixels with the specified <see cref="SearchOptions"/>, 
    /// accepts a <see cref="IPixel{T}"/> type argument to allow user-defined RGBA providers
    /// 
    /// <para>Any color with 0 alpha is considered a wildcard color and allowed to pass</para>
    /// <para>The arrays are required to be in row-major order for the method to work</para>
    /// </summary>
    /// <param name="input">The input pixel array</param>
    /// <param name="source">The source pixel array</param>
    /// <param name="options">The searching options</param>
    /// <param name="location">Output location if a match is found</param>
    /// <returns><c>true</c> if a match is found</returns>
    /// <exception cref="ArgumentOutOfRangeException">Throws if source, input or clip has invalid sizes</exception>
    public static bool FindPixels<T>(PixelArray<T> input, PixelArray<T> source, in SearchOptions options, out (int x, int y) location) where T : unmanaged, IPixel<T> {
        return FindPixels(input, source, in options, (0, 0, source.Width, source.Height), out location);
    }

    /// <summary>
    /// Compares 2 arrays of pixels with the specified <see cref="SearchOptions"/>, 
    /// accepts a <see cref="IPixel{T}"/> type argument to allow user-defined RGBA providers. 
    ///
    /// <para>Any color with 0 alpha is considered a wildcard color and allowed to pass</para>
    /// <para>The arrays are required to be in row-major order for the method to work</para>
    /// </summary>
    /// <param name="input">The input pixel array</param>
    /// <param name="source">The source pixel array</param>
    /// <param name="options">The searching options</param>
    /// <param name="clip">Clip rectangle that allows searching a slice of the source array</param>
    /// <param name="location">Output location if a match is found</param>
    /// <returns><c>true</c> if a match is found</returns>
    /// <exception cref="ArgumentOutOfRangeException">Throws if source, input or clip has invalid sizes</exception>
    public static bool FindPixels<T>(PixelArray<T> input, PixelArray<T> source, in SearchOptions options, (int x, int y, int width, int height) clip, out (int x, int y) location) where T : unmanaged, IPixel<T> {
 
        location = (0, 0);

        // Sanity checks

        if (source.Width < input.Width || source.Height < input.Height || source.Pixels.Length < input.Pixels.Length) {
            throw new ArgumentOutOfRangeException(nameof(source), "Source must be larger than input");
        }

        if (source.Size < (uint)(clip.width * clip.height)) {
            throw new ArgumentOutOfRangeException(nameof(source), "Source cannot be smaller than clip size");
        }

        // calculate max invalid pixels
        int maxInvalidPixels = (int)(input.Pixels.Length * options.ImageTolerance);

        // calculate pixel tolerance, squared
        int pixelToleranceLevel = (int)(options.PixelTolerance * byte.MaxValue);
        pixelToleranceLevel *= pixelToleranceLevel;

        int endX = clip.x + clip.width - input.Width;
        int endY = clip.y + clip.height - input.Height;

        for (int currentY = clip.y; currentY < endY; currentY++) {
            for(int currentX = clip.x; currentX < endX; currentX++) {
                if (MatchesAt(input, source, currentX, currentY, maxInvalidPixels, pixelToleranceLevel)) {
                    location = (currentX, currentY);
                    return true;
                }        
            }
        }

        return false;
    }

    /// <summary>
    /// Searches through a region of pixels with a specified input and tolerance
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="input">The input pixel array</param>
    /// <param name="source">The source pixel array</param>
    /// <param name="offsetX">Offset X coordinate for searching the source</param>
    /// <param name="offsetY">Offset Y coordinate for searching the source</param>
    /// <param name="maxInvalidPixels">Max amount of invalid pixels</param>
    /// <param name="tolerance">Max difference between pixels, squared</param>
    /// <returns><c>true</c> if the pixels match</returns>
    private static bool MatchesAt<T>(PixelArray<T> input, PixelArray<T> source, int offsetX, int offsetY, int maxInvalidPixels, int tolerance) where T : unmanaged, IPixel<T> {

        ReadOnlySpan<T> needle = input.Pixels;
        ReadOnlySpan<T> haystack = source.Pixels;

        int invalidPixels = 0;

        for (int y = 0; y < input.Height; y++) {
            for (int x = 0; x < input.Width; x++) {

                T pixel = needle[x + y * input.Width];
                T cmp = haystack[(offsetX + x) + (y + offsetY) * source.Width];

                if (pixel.A == 0) {
                    maxInvalidPixels--;
                    continue;
                }

                if (pixel.Equals(cmp) || (tolerance > 0 && Difference(pixel,cmp) <= tolerance)) {
                    continue;
                }

                if (++invalidPixels >= maxInvalidPixels) {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Calculates the difference between 2 pixels by converting the pixels to 4D vectors and checking their distance
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Difference<T>(T left, T right) where T : unmanaged, IPixel<T> {
        return Vector4.DistanceSquared(new Vector4(left.R, left.G, left.B, left.A), new Vector4(right.R, right.G, right.B, right.A));
    }
}