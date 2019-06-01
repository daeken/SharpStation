using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MoreLinq.Extensions;
using PrettyPrinter;

namespace SharpStation {
	public class CueImage : CdImage {
		readonly Stream Fp;
		
		public CueImage(string fn) {
			Stream? fp = null;
			foreach(var line in File.ReadLines(fn).Select(x => x.Trim())) {
				var elems = Regex.Replace(line, " +", " ").Split(" ");
				if(elems.Length == 0) return;
				switch(elems[0].ToUpper()) {
					case "FILE":
						var bfn = line.Split('"')[1].Split('"')[0];
						$"File is '{bfn}'".Debug();
						fp = File.OpenRead(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(fn)), bfn));
						break;
					case "TRACK":
						var tnum = int.Parse(elems[1]);
						var mode = elems[2].ToUpper();
						$"Track {tnum} -- {mode}".Debug();
						break;
					case "INDEX":
						var inum = int.Parse(elems[1]);
						var msf = elems[2].Split(':').Select(int.Parse).ToArray();
						$"Index {inum} -- {msf.ToPrettyString()}".Debug();
						break;
				}
			}
			
			Fp = fp ?? throw new NotSupportedException("Cue file missing binary file reference");
		}

		public override bool ReadSector(Span<byte> data, (int M, int S, int F) msf) {
			var (m, s, f) = msf;
			var sectorIndex = 60 * 75 * m + 75 * s + f - 150;
			$"Reading sector from index {sectorIndex}".Debug();
			Fp.Seek(2352 * sectorIndex, SeekOrigin.Begin);
			Fp.Read(data);
			return true;
		}
	}
}