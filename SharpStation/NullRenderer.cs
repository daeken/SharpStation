using System;

namespace SharpStation {
	public class NullRenderer : IRenderer {
		public void KickOff(Action main) => main();

		public void DrawSolidTriangle(Color color, Coord a, Coord b, Coord c) { }
		public void DrawShadedTriangle(Coord a, Color ac, Coord b, Color bc, Coord c, Color cc) {}
		public void DrawSolidQuad(Color color, Coord a, Coord b, Coord c, Coord d) {}
		public void DrawShadedQuad(Coord a, Color ac, Coord b, Color bc, Coord c, Color cc, Coord d, Color dc) {}
		public void EndFrame() {}
	}
}