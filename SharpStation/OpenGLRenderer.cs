using System;
using System.Collections.Generic;
using System.Threading;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SharpStation {
	public class OpenGLRenderer : IRenderer {
		bool Ready;
		readonly Queue<Action> Commands = new Queue<Action>();
		
		public void KickOff(Action main) {
			new Thread(() => main()).Start();
			var window = new GameWindow(640, 480, GraphicsMode.Default, "SharpStation", GameWindowFlags.FixedWindow);
			GL.Viewport(0, 0, 640, 480);
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadIdentity();
			GL.Ortho(0, 640, 480, 0, 0, 1);
			GL.MatrixMode(MatrixMode.Modelview);
			
			GL.Disable(EnableCap.CullFace);
			GL.ClearColor(0, 0, 0, 1);
			GL.Clear(ClearBufferMask.ColorBufferBit);
			window.SwapBuffers();
			window.RenderFrame += (_, __) => {
				for(var i = 0; i < 10; ++i) {
					lock(this)
						if(Ready) {
							GL.Clear(ClearBufferMask.ColorBufferBit);
							while(Commands.TryDequeue(out var func))
								func();
							window.SwapBuffers();
							Ready = false;
						}
					Thread.Sleep(1);
				}
			};
			window.Closing += (_, __) => Environment.Exit(0);
			window.Run(30, 30);
		}

		void Add(Action func) {
			lock(this)
				Commands.Enqueue(func);
		}

		public void DrawSolidTriangle(Color color, Coord a, Coord b, Coord c) => Add(() => {
			GL.Color4(color.R, color.G, color.B, color.A);
			GL.Begin(PrimitiveType.Triangles);
			GL.Vertex2(a.X, a.Y);
			GL.Vertex2(b.X, b.Y);
			GL.Vertex2(c.X, c.Y);
			GL.End();
		});

		public void DrawShadedTriangle(Coord a, Color ac, Coord b, Color bc, Coord c, Color cc) => Add(() => {
			GL.Begin(PrimitiveType.Triangles);
			GL.Color4(ac.R, ac.G, ac.B, ac.A);
			GL.Vertex2(a.X, a.Y);
			GL.Color4(bc.R, bc.G, bc.B, bc.A);
			GL.Vertex2(b.X, b.Y);
			GL.Color4(cc.R, cc.G, cc.B, cc.A);
			GL.Vertex2(c.X, c.Y);
			GL.End();
		});
		
		public void DrawSolidQuad(Color color, Coord a, Coord b, Coord c, Coord d) {
			DrawSolidTriangle(color, a, b, c);
			DrawSolidTriangle(color, b, d, c);
		}

		public void DrawShadedQuad(Coord a, Color ac, Coord b, Color bc, Coord c, Color cc, Coord d, Color dc) {
			DrawShadedTriangle(a, ac, b, bc, c, cc);
			DrawShadedTriangle(b, bc, d, dc, c, cc);
		}

		public void EndFrame() {
			lock(this)
				Ready = true;
			while(true) {
				lock(this)
					if(!Ready) break;
				Thread.Sleep(1);
			}
		}
	}
}