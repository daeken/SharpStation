using System;
using System.Collections.Generic;
using System.Linq;
using static SharpStation.Globals;

namespace SharpStation {
	public class EventSystem {
		readonly SortedList<uint, List<Action>> Upcoming = new SortedList<uint, List<Action>>();
		public uint NextTimestamp = 0xFFFFFFFF;

		public void Add(uint time, Action func) {
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
			foreach(var kv in Upcoming) {
				if(kv.Key > Timestamp) break;
				changed = true;
				foreach(var func in kv.Value)
					func();
				Upcoming.Remove(kv.Key);
			}
			if(changed)
				NextTimestamp = Upcoming.Count == 0 ? 0xFFFFFFFF : Upcoming.First().Key;
			return Cpu.Running;
		}
	}
}