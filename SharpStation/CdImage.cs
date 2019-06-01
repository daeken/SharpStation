using System;

namespace SharpStation {
	public abstract class CdImage {
		public abstract bool ReadSector(Span<byte> data, (int M, int S, int F) msf);
	}
}