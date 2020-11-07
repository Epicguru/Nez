using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using static Nez.Tools.Atlases.SpriteAtlasPacker;

namespace Nez.Tools.Atlases
{
	public class ImagePacker
	{
		// various properties of the resulting image
		private bool requirePow2, requireSquare;
		private int padding;
		private int outputWidth, outputHeight;

		// the input list of image files
		private List<string> files;

		// some dictionaries to hold the image sizes and destination rectangles
		private readonly Dictionary<string, Size> imageSizes = new Dictionary<string, Size>();
		private readonly Dictionary<string, Rectangle> imagePlacement = new Dictionary<string, Rectangle>();

		/// <summary>
		/// Packs a collection of images into a single image.
		/// </summary>
		/// <param name="imageFiles">The list of file paths of the images to be combined.</param>
		/// <param name="requirePowerOfTwo">Whether or not the output image must have a power of two size.</param>
		/// <param name="requireSquareImage">Whether or not the output image must be a square.</param>
		/// <param name="maximumWidth">The maximum width of the output image.</param>
		/// <param name="maximumHeight">The maximum height of the output image.</param>
		/// <param name="imagePadding">The amount of blank space to insert in between individual images.</param>
		/// <param name="outputImage">The resulting output image.</param>
		/// <param name="outputMap">The resulting output map of placement rectangles for the images.</param>
		/// <returns>0 if the packing was successful, error code otherwise.</returns>
		public int PackImage(
			IEnumerable<string> imageFiles, 
			bool requirePowerOfTwo, 
			bool requireSquareImage, 
			int maximumWidth,
			int maximumHeight,
			int imagePadding,
			out Bitmap outputImage, 
			out Dictionary<string, Rectangle> outputMap,
			Action<string> step = null)
		{
			files = new List<string>(imageFiles);
			requirePow2 = requirePowerOfTwo;
			requireSquare = requireSquareImage;
			outputWidth = maximumWidth;
			outputHeight = maximumHeight;
			padding = imagePadding;

			outputImage = null;
			outputMap = null;

			// make sure our dictionaries are cleared before starting
			imageSizes.Clear();
			imagePlacement.Clear();

			// get the sizes of all the images
			foreach (var image in files)
			{
				step?.Invoke($"Loading {Path.GetFileName(image)}...");
				using (var bitmap = Bitmap.FromFile(image) as Bitmap)
				{
					if (bitmap == null)
						return (int)FailCode.FailedToLoadImage;
					imageSizes.Add(image, bitmap.Size);
				}
			}

			// sort our files by file size so we place large sprites first
			step?.Invoke("Sorting images...");
			files.Sort(
				(f1, f2) =>
				{
					Size b1 = imageSizes[f1];
					Size b2 = imageSizes[f2];

					int c = -b1.Width.CompareTo(b2.Width);
					if (c != 0)
						return c;

					c = -b1.Height.CompareTo(b2.Height);
					if (c != 0)
						return c;

					return f1.CompareTo(f2);
				});

			// try to pack the images
			if (!PackImageRectangles(step))
				return (int)FailCode.FailedToPackImage;

			// make our output image
			outputImage = CreateOutputImage(step);
			if (outputImage == null)
				return (int)FailCode.FailedToSaveImage;

			step?.Invoke("Generating meta...");
				
			// go through our image placements and replace the width/height found in there with
			// each image's actual width/height (since the ones in imagePlacement will have padding)
			var keys = new string[imagePlacement.Keys.Count];
			imagePlacement.Keys.CopyTo(keys, 0);
			foreach (var k in keys)
			{
				// get the actual size
				var s = imageSizes[k];

				// get the placement rectangle
				var r = imagePlacement[k];

				// set the proper size
				r.Width = s.Width;
				r.Height = s.Height;

				// insert back into the dictionary
				imagePlacement[k] = r;
			}

			// copy the placement dictionary to the output
			outputMap = new Dictionary<string, Rectangle>();
			foreach (var pair in imagePlacement)
			{
				outputMap.Add(pair.Key, pair.Value);
			}

			// clear our dictionaries just to free up some memory
			imageSizes.Clear();
			imagePlacement.Clear();

			return 0;
		}

		// This method does some trickery type stuff where we perform the TestPackingImages method over and over, 
		// trying to reduce the image size until we have found the smallest possible image we can fit.
		private bool PackImageRectangles(Action<string> step = null)
		{
			// create a dictionary for our test image placements
			Dictionary<string, Rectangle> testImagePlacement = new Dictionary<string, Rectangle>();

			// get the size of our smallest image
			int smallestWidth = int.MaxValue;
			int smallestHeight = int.MaxValue;
			foreach (var size in imageSizes)
			{
				smallestWidth = Math.Min(smallestWidth, size.Value.Width);
				smallestHeight = Math.Min(smallestHeight, size.Value.Height);
			}

			// we need a couple values for testing
			int testWidth = outputWidth;
			int testHeight = outputHeight;

			bool shrinkVertical = false;
			int i = 0;

			// just keep looping...
			while (true)
			{
				step?.Invoke($"Pack attempt {i + 1}: {testWidth}x{testHeight}");
				i++;
				// make sure our test dictionary is empty
				testImagePlacement.Clear();

				// try to pack the images into our current test size
				if (!TestPackingImages(testWidth, testHeight, testImagePlacement))
				{
					// if that failed...

					// if we have no images in imagePlacement, i.e. we've never succeeded at PackImages,
					// show an error and return false since there is no way to fit the images into our
					// maximum size texture
					if (imagePlacement.Count == 0)
						return false;

					// otherwise return true to use our last good results
					if (shrinkVertical)
						return true;

					shrinkVertical = true;
					testWidth += smallestWidth + padding + padding;
					testHeight += smallestHeight + padding + padding;
					continue;
				}

				// clear the imagePlacement dictionary and add our test results in
				imagePlacement.Clear();
				foreach (var pair in testImagePlacement)
					imagePlacement.Add(pair.Key, pair.Value);

				// figure out the smallest bitmap that will hold all the images
				testWidth = testHeight = 0;
				foreach (var pair in imagePlacement)
				{
					testWidth = Math.Max(testWidth, pair.Value.Right);
					testHeight = Math.Max(testHeight, pair.Value.Bottom);
				}

				// subtract the extra padding on the right and bottom
				if (!shrinkVertical)
					testWidth -= padding;
				testHeight -= padding;

				// if we require a power of two texture, find the next power of two that can fit this image
				if (requirePow2)
				{
					testWidth = MiscHelper.FindNextPowerOfTwo(testWidth);
					testHeight = MiscHelper.FindNextPowerOfTwo(testHeight);
				}

				// if we require a square texture, set the width and height to the larger of the two
				if (requireSquare)
				{
					int max = Math.Max(testWidth, testHeight);
					testWidth = testHeight = max;
				}

				// if the test results are the same as our last output results, we've reached an optimal size,
				// so we can just be done
				if (testWidth == outputWidth && testHeight == outputHeight)
				{
					if (shrinkVertical)
						return true;

					shrinkVertical = true;
				}

				// save the test results as our last known good results
				outputWidth = testWidth;
				outputHeight = testHeight;

				// subtract the smallest image size out for the next test iteration
				if (!shrinkVertical)
					testWidth -= smallestWidth;
				testHeight -= smallestHeight;
			}
		}

		private bool TestPackingImages(int testWidth, int testHeight, Dictionary<string, Rectangle> testImagePlacement)
		{
			// create the rectangle packer
			ArevaloRectanglePacker rectanglePacker = new ArevaloRectanglePacker(testWidth, testHeight);

			foreach (var image in files)
			{
				// get the bitmap for this file
				Size size = imageSizes[image];

				// pack the image
				Point origin;
				if (!rectanglePacker.TryPack(size.Width + padding * 2, size.Height + padding * 2, out origin))
				{
					return false;
				}

				// add the destination rectangle to our dictionary
				testImagePlacement.Add(image, new Rectangle(origin.X + padding, origin.Y + padding, size.Width + padding * 2, size.Height + padding * 2));
			}

			return true;
		}

		private Bitmap CreateOutputImage(Action<string> step = null)
		{
			try
			{
				step?.Invoke("Creating output image...");
				var outputImage = new Bitmap(outputWidth, outputHeight, PixelFormat.Format32bppArgb);

				// Fast mode uses Graphics.DrawImage instead of manually copying pixels.
				// Despite what the original author commented about antialiasing issues,
				// in my testing I found the output to be identical but the processing 
				// massively faster.
				bool FAST_MODE = true;
				Graphics g = null;
				if (FAST_MODE)
					g = Graphics.FromImage(outputImage);

				// draw all the images into the output image
				foreach (var image in files)
				{
					var location = imagePlacement[image];
					step?.Invoke($"Loading {Path.GetFileName(image)}...");
					using (var bitmap = Bitmap.FromFile(image) as Bitmap)
					{
						if (bitmap == null)
						{
							g?.Dispose();
							return null;
						}

						step?.Invoke($"Writing {Path.GetFileName(image)}...");

						// copy pixels over to avoid antialiasing or any other side effects of drawing
						// the subimages to the output image using Graphics

						if (!FAST_MODE)
						{
							for (int x = 0; x < bitmap.Width; x++)
							{
								for (int y = 0; y < bitmap.Height; y++)
								{
									outputImage.SetPixel(location.X + x, location.Y + y, bitmap.GetPixel(x, y));
								}

								if (x % 16 == 0)
									step?.Invoke($"Writing {Path.GetFileName(image)} {(float)x / bitmap.Width * 100f:F0}% ...");

							}
						}
						else
						{
							Rectangle dest = new Rectangle(location.X, location.Y, bitmap.Width, bitmap.Height);
							Rectangle src = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
							g.DrawImage(bitmap, dest, src, GraphicsUnit.Pixel);
							int amount = padding;
							if (padding >= 1)
							{
								var left = new Rectangle(location.X - amount, location.Y, amount, bitmap.Height);
								var leftSrc = new Point(0, 0);
								Blit(outputImage, bitmap, left, leftSrc, false);

								var right = new Rectangle(location.X + bitmap.Width, location.Y, amount, bitmap.Height);
								var rightSrc = new Point(bitmap.Width - 1, 0);
								Blit(outputImage, bitmap, right, rightSrc, false);

								var top = new Rectangle(location.X, location.Y - amount, bitmap.Width, amount);
								var topSrc = new Point(0, 0);
								Blit(outputImage, bitmap, top, topSrc, true);

								var bot = new Rectangle(location.X, location.Y + bitmap.Height, bitmap.Width, amount);
								var botSrc = new Point(0, bitmap.Height - 1);
								Blit(outputImage, bitmap, bot, botSrc, true);
							}
						}
					}
				}

				g?.Dispose();
				return outputImage;
			}
			catch
			{
				return null;
			}
		}

		private void Blit(Bitmap dstBitmap, Bitmap srcBitmap, Rectangle dest, Point src, bool horizontal)
		{
			int ix = 0, iy = 0;
			for (int x = dest.X; x < dest.Right; x++)
			{
				for (int y = dest.Y; y < dest.Bottom; y++)
				{
					int sx = horizontal ? src.X + ix : src.X;
					int sy = horizontal ? src.Y : src.Y + iy;
					Color col = srcBitmap.GetPixel(sx, sy);

					dstBitmap.SetPixel(x, y, col);
					iy++;
				}

				ix++;
				iy = 0;
			}
		}
	}
}