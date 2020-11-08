using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace Nez.Tools.Atlases
{
	public class SpriteAtlasPacker
	{
		public enum FailCode
		{
			FailedParsingArguments = 1,
			NoImages,
			ImageNameCollision,
			FailedToLoadImage,
			FailedToPackImage,
			FailedToSaveImage
		}

		public class Config
		{
			public string AtlasOutputFile;
			public string MapOutputFile;
			public int AtlasMaxWidth = Constants.DefaultMaximumSheetWidth;
			public int AtlasMaxHeight = Constants.DefaultMaximumSheetHeight;
			public int Padding = Constants.DefaultImagePadding;
			public bool IsPowerOfTwo;
			public bool IsSquare;
			public bool RecurseSubdirectories;
			public float OriginX = Constants.DefaultOrigin;
			public float OriginY = Constants.DefaultOrigin;
			public bool DontCreateAnimations = false;
			public int FrameRate = Constants.DefaultFrameRate;
			public string[] InputPaths;
			public bool OutputLua;
		}

		public static int PackSprites(Config config, Action<string> step = null, Func<string, bool> filePicker = null, string namesRelativeTo = null)
		{
			// compile a list of images
			var animations = new Dictionary<string, List<string>>();
			var images = new List<string>();
			step?.Invoke("Finding sprites...");
			FindImages(config, images, animations, filePicker);

			// make sure we found some images
			if (images.Count == 0)
			{
				System.Console.WriteLine("No images to pack.");
				return (int)FailCode.NoImages;
			}

			//step?.Invoke("Checking for duplicate names...");
			// make sure no images have the same name if we're building a map
			//for (int i = 0; i < images.Count; i++)
			//{
			//	var str1 = Path.GetFileNameWithoutExtension(images[i]);
			//	for (int j = i + 1; j < images.Count; j++)
			//	{
			//		var str2 = Path.GetFileNameWithoutExtension(images[j]);
			//		if (str1 == str2)
			//		{
			//			System.Console.WriteLine("Two images have the same name: {0} = {1}", images[i], images[j]);
			//			return (int)FailCode.ImageNameCollision;
			//		}
			//	}
			//}

			// generate our output
			var imagePacker = new ImagePacker();

			// pack the image, generating a map only if desired
			int result = imagePacker.PackImage(images, config.IsPowerOfTwo, config.IsSquare, config.AtlasMaxWidth, config.AtlasMaxHeight, config.Padding, out Bitmap outputImage, out Dictionary<string, Rectangle> outputMap, step);
			if (result != 0)
			{
				System.Console.WriteLine("There was an error making the image sheet.");
				return result;
			}

			// try to save using our exporters
			if (File.Exists(config.AtlasOutputFile))
			{
				step?.Invoke("Removing existing output file...");
				File.Delete(config.AtlasOutputFile);
			}

			step?.Invoke("Saving output file...");
			var imageExtension = Path.GetExtension(config.AtlasOutputFile).Substring(1).ToLower();
			switch (imageExtension)
			{
				case "png":
					outputImage.Save(config.AtlasOutputFile, ImageFormat.Png);
					break;
				case "jpg":
					outputImage.Save(config.AtlasOutputFile, ImageFormat.Jpeg);
					break;
				case "bmp":
					outputImage.Save(config.AtlasOutputFile, ImageFormat.Bmp);
					break;
				default:
					throw new Exception("Invalid image format for output image");
			}

			if(config.OutputLua)
				config.MapOutputFile = config.MapOutputFile.Replace( ".atlas", ".lua" );

			if (File.Exists(config.MapOutputFile))
			{
				step?.Invoke("Deleting existing meta file...");
				File.Delete(config.MapOutputFile);
			}

			step?.Invoke("Saving meta file...");
			if (namesRelativeTo == null)
				namesRelativeTo = config.InputPaths[0];
			
			if (config.OutputLua)
				LuaMapExporter.Save(config.MapOutputFile, outputMap, animations, outputImage.Width, outputImage.Height, config );
			else
				AtlasMapExporter.Save(config.MapOutputFile, outputMap, animations, config, namesRelativeTo);

			step?.Invoke("Done");
			return 0;
		}

		static void FindImages(Config arguments, List<string> images, Dictionary<string, List<string>> animations, Func<string, bool> filePicker)
		{
			for(int i = 0; i < arguments.InputPaths.Length; i++)
			{
				string str = arguments.InputPaths[i];
				string dirName = Path.GetFileName(str);
				if (dirName.StartsWith("[Atlas]") && i > 0)
					continue;

				if (Directory.Exists(str))
				{
					foreach (var file in Directory.GetFiles(str))
					{
						if (MiscHelper.IsImageFile(file) && (filePicker?.Invoke(file) ?? true))
							images.Add(file);
					}

					foreach (var dir in Directory.GetDirectories(str))
					{
						if (Path.GetFileName(dir).StartsWith("[Atlas]"))
							continue;

						FindImagesRecursively(dir, images, animations, !arguments.DontCreateAnimations, filePicker);
					}
				}
				else if (MiscHelper.IsImageFile(str) && (filePicker?.Invoke(str) ?? true))
				{
					images.Add(str);
				}
			}
		}

		static void FindImagesRecursively(string dir, List<string> images, Dictionary<string, List<string>> animations, bool createAnimations, Func<string, bool> filePicker)
		{
			var animationFrames = new List<string>();
			foreach (var file in Directory.GetFiles(dir).OrderBy(_ => _))
			{
				if (MiscHelper.IsImageFile(file) && (filePicker?.Invoke(file) ?? true))
				{
					images.Add(file);
					animationFrames.Add(file);
				}
			}

			if (createAnimations && animationFrames.Count > 0)
				animations.Add( Path.GetFileName( dir ), animationFrames );

			foreach (var subdir in Directory.GetDirectories(dir))
			{
				if(!Path.GetFileName(subdir).StartsWith("[Atlas]"))
					FindImagesRecursively(subdir, images, animations, createAnimations, filePicker);
			}
		}

	}
}
