using System;
using System.Collections.Generic;
using Nez.Textures;

namespace Nez.Sprites
{
	public class SpriteAtlas : IDisposable
	{
		public string[] Names;
		public Sprite[] Sprites;

		public string[] AnimationNames;
		public SpriteAnimation[] SpriteAnimations;

		private Dictionary<string, Sprite> namedSprites;

		public Sprite GetSprite(string name)
		{
			// Changed to a dictionary to get O(1) instead of the previous O(n) complexity.
			if (namedSprites == null || namedSprites.Count != Names.Length)
			{
				namedSprites = new Dictionary<string, Sprite>();
				for(int i = 0; i < Names.Length; i++)
				{
					namedSprites.Add(Names[i], Sprites[i]);
				}
			}

			return namedSprites.TryGetValue(name, out var spr) ? spr : null;
		}

		public SpriteAnimation GetAnimation(string name)
		{
			var index = Array.IndexOf(AnimationNames, name);
			return SpriteAnimations[index];
		}

		void IDisposable.Dispose()
		{
			// all our Sprites use the same Texture so we only need to dispose one of them
			if (Sprites != null)
			{
				Sprites[0].Texture2D.Dispose();
				Sprites = null;
			}
		}
	}
}
