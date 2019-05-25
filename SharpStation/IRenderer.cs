using System;

namespace SharpStation {
	public struct Coord {
		public int X, Y;

		public Coord(int x, int y) {
			X = x;
			Y = y;
		}

		public static implicit operator Coord((int, int) coord) => new Coord(coord.Item1, coord.Item2);
		public static implicit operator Coord((uint, uint) coord) => new Coord((int) coord.Item1, (int) coord.Item2);
	}

	public struct Color {
		public byte R, G, B, A;

		public Color(byte r, byte g, byte b, byte a = 255) {
			R = r;
			G = g;
			B = b;
			A = a;
		}
	}
	
	public interface IRenderer {
		void KickOff(Action main);
		void DrawSolidTriangle(Color color, Coord a, Coord b, Coord c);
		void DrawShadedTriangle(Coord a, Color ac, Coord b, Color bc, Coord c, Color cc);
		void DrawSolidQuad(Color color, Coord a, Coord b, Coord c, Coord d);
		void DrawShadedQuad(Coord a, Color ac, Coord b, Color bc, Coord c, Color cc, Coord d, Color dc);
		void EndFrame();
	}
}