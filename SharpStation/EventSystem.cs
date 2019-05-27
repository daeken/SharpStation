using System;
using System.Collections.Generic;
using System.Linq;
using static SharpStation.Globals;

namespace SharpStation {
	public class EventSystem {
		readonly SortedList<ulong, List<Action>> Upcoming = new SortedList<ulong, List<Action>>();
		public ulong NextTimestamp = 0xFFFFFFFFFFFFFFFF;

		public void Add(ulong time, Action func) {
			if(Upcoming.TryGetValue(time, out var list))
				list.Add(func);
			else
				Upcoming[time] = new List<Action> { func };
			if(NextTimestamp > time)
				NextTimestamp = time;
		}

		public bool RunEvents() {
			var changed = false;
			// ReSharper disable once UseDeconstruction // Deconstruction fucks up nullable reference type stuff
			foreach(var kv in Upcoming.ToArray()) {
				if(kv.Key > Timestamp) break;
				changed = true;
				foreach(var func in kv.Value)
					func();
				Upcoming.Remove(kv.Key);
			}
			if(changed)
				NextTimestamp = Upcoming.Count == 0 ? 0xFFFFFFFFFFFFFFFF : Upcoming.First().Key;
			return Cpu.Running;
		}
	}
}