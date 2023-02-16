using PixelSearch2;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

#if WINDOWS
#pragma warning disable CA1416 // Validate platform compatibility
#endif

// first get the images to search
using var imageBmp = new Bitmap(@"TestSearch.png");
using var screenBmp = new Bitmap(@"TestInput.png");

// to access the raw pixel data of the bitmaps, they need to be locked
var imgData = imageBmp.LockBits(new Rectangle(Point.Empty, imageBmp.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
var screenData = screenBmp.LockBits(new Rectangle(Point.Empty, screenBmp.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);


// wrap the pixel data in a pixel array
PixelArray<Pixel> input = new PixelArray<Pixel>(imgData.Scan0, imgData.Width, imgData.Height);
PixelArray<Pixel> source = new PixelArray<Pixel>(screenData.Scan0, screenBmp.Width, screenBmp.Height);

// now that we have access to the pixel data, we can finally perform the pixel search
if (PixelSearch.FindPixels(
    input,
    source,
    new SearchOptions(pixelTolerance: 0.15f, imageTolerance: 0.25f), // searching options
    out var location
)) {
    Console.WriteLine($"Image was found at (x: {location.x}, y: {location.y})");
} else {
    Console.WriteLine("The image could not be found from the screen.");
}

// its important to unlock the memory after the operation
imageBmp.UnlockBits(imgData);
screenBmp.UnlockBits(screenData);