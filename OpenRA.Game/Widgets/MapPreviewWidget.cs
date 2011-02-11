#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	public class MapPreviewWidget : Widget
	{
		public Func<Map> Map = () => null;
		public Func<Dictionary<int2, Color>> SpawnColors = () => new Dictionary<int2, Color>();
		static Cache<Map,Bitmap> PreviewCache = new Cache<Map, Bitmap>(stub => Minimap.RenderMapPreview( new Map( stub.Path )));

		public MapPreviewWidget() : base() { }
		protected MapPreviewWidget(MapPreviewWidget other)
			: base(other)
		{
			lastMap = other.lastMap;
			Map = other.Map;
			SpawnColors = other.SpawnColors;
		}
		public override Widget Clone() { return new MapPreviewWidget(this); }

		public int2 ConvertToPreview(Map map, int2 point)
		{
			return new int2(MapRect.X + (int)(PreviewScale*(point.X - map.Bounds.Left)) , MapRect.Y + (int)(PreviewScale*(point.Y - map.Bounds.Top)));
		}
		
		Sheet mapChooserSheet;
		Sprite mapChooserSprite;
		Map lastMap;
		Rectangle MapRect;
		float PreviewScale = 0;
		static Sprite UnownedSpawn = null;
		static Sprite OwnedSpawn = null;

		public override void DrawInner()
		{
			if (UnownedSpawn == null)
				UnownedSpawn = ChromeProvider.GetImage("spawnpoints", "unowned");
			if (OwnedSpawn == null)
				OwnedSpawn = ChromeProvider.GetImage("spawnpoints", "owned");
			
			var map = Map();
			if( map == null ) return;
			
			if (lastMap != map)
			{
				lastMap = map;

				// Update image data
				var preview = PreviewCache[map];
				if( mapChooserSheet == null || mapChooserSheet.Size.Width != preview.Width || mapChooserSheet.Size.Height != preview.Height )
					mapChooserSheet = new Sheet(new Size( preview.Width, preview.Height ) );

				mapChooserSheet.Texture.SetData( preview );
				mapChooserSprite = new Sprite( mapChooserSheet, new Rectangle( 0, 0, map.Bounds.Width, map.Bounds.Height ), TextureChannel.Alpha );
				
				// Update map rect
				PreviewScale = Math.Min(RenderBounds.Width * 1.0f / map.Bounds.Width, RenderBounds.Height * 1.0f / map.Bounds.Height);
				var size = Math.Max(map.Bounds.Width, map.Bounds.Height);
				var dw = (int)(PreviewScale * (size - map.Bounds.Width)) / 2;
				var dh = (int)(PreviewScale * (size - map.Bounds.Height)) / 2;
				MapRect = new Rectangle(RenderBounds.X + dw, RenderBounds.Y + dh, (int)(map.Bounds.Width * PreviewScale), (int)(map.Bounds.Height * PreviewScale));
			}

			Game.Renderer.RgbaSpriteRenderer.DrawSprite( mapChooserSprite,
				new float2(MapRect.Location),
				new float2( MapRect.Size ) );

			// Overlay spawnpoints
			var colors = SpawnColors();
			foreach (var p in map.SpawnPoints)
			{
				var pos = ConvertToPreview(map, p);
				var sprite = UnownedSpawn;
				var offset = new int2(-UnownedSpawn.bounds.Width/2, -UnownedSpawn.bounds.Height/2);

				if (colors.ContainsKey(p))
				{
					sprite = OwnedSpawn;
					offset = new int2(-OwnedSpawn.bounds.Width/2, -OwnedSpawn.bounds.Height/2);
					WidgetUtils.FillRectWithColor(new Rectangle(pos.X + offset.X + 2, pos.Y + offset.Y + 2, 12, 12), colors[p]);
				}
				Game.Renderer.RgbaSpriteRenderer.DrawSprite(sprite, pos + offset);
			}
		}
	}
}
