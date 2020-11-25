using System;
using Microsoft.Xna.Framework;
using Nez.PhysicsShapes;


namespace Nez
{
	/// <summary>
	/// Polygons should be defined in clockwise fashion.
	/// </summary>
	public class PolygonCollider : Collider
	{
		/// <summary>
		/// Creates a new polygon collider from the array of vertices. The vertices should be listed in clockwise order.
		/// </summary>
		/// <param name="points">Points.</param>
		/// <param name="centerPoints">If true, the points will be centered with the difference being applied to the localOffset.</param>
		public PolygonCollider(Vector2[] points, bool centerPoints = true)
		{
			// first and last point must not be the same. we want an open polygon
			var isPolygonClosed = points[0] == points[points.Length - 1];

			if (isPolygonClosed)
				Array.Resize(ref points, points.Length - 1);

			var center = centerPoints ? Polygon.FindPolygonCenter(points) : Vector2.Zero;
			SetLocalOffset(center);
			if(centerPoints)
				Polygon.RecenterPolygonVerts(points);
			Shape = new Polygon(points);
		}

		public PolygonCollider(int vertCount, float radius)
		{
			Shape = new Polygon(vertCount, radius);
		}

		public PolygonCollider() : this(6, 40)
		{
		}

		public override void DebugRender(Batcher batcher)
		{
			var poly = Shape as Polygon;
			batcher.DrawHollowRect(Bounds, Debug.Colors.ColliderBounds, Debug.Size.LineSizeMultiplier);
			batcher.DrawPolygon(Shape.position, poly.Points, Debug.Colors.ColliderEdge, true,
				Debug.Size.LineSizeMultiplier);
			batcher.DrawPixel(Entity.Transform.Position, Debug.Colors.ColliderPosition,
				4 * Debug.Size.LineSizeMultiplier);
			batcher.DrawPixel(Shape.position, Debug.Colors.ColliderCenter, 2 * Debug.Size.LineSizeMultiplier);

			// Normal debug code
			//for( var i = 0; i < poly.points.Length; i++ )
			//{
			//	Vector2 p2;
			//	var p1 = poly.points[i];
			//	if( i + 1 >= poly.points.Length )
			//		p2 = poly.points[0];
			//	else
			//		p2 = poly.points[i + 1];
			//	var perp = Vector2Ext.perpendicular( ref p1, ref p2 );
			//	Vector2Ext.normalize( ref perp );
			//	var mp = Vector2.Lerp( p1, p2, 0.5f ) + poly.position;
			//	batcher.drawLine( mp, mp + perp * 10, Color.White );
			//}
		}
	}
}