using System;
using UnityEngine;

namespace AudioStudio
{
	[Serializable]
	public struct BarAndBeat
	{
		public int Bar, Beat;

		public BarAndBeat(int bar, int beat) //constructor
		{
			Bar = bar;
			Beat = beat;
		}

		public BarAndBeat Negative(int beatsPerBar)
		{
			Bar = -Bar;
			if (Beat > 0)
			{
				Bar--;
				Beat = beatsPerBar - Beat;
			}
			return this;
		}

		public float ToBars(int beatsPerBar)
		{
			return Bar + Beat * 1f / beatsPerBar;
		}
		
		public int ToBeats(int beatsPerBar)
		{
			return Bar * beatsPerBar + Beat;
		}

		public static BarAndBeat ToBarAndBeat(float beats, int beatsPerBar)
		{
			var beatsInt = Mathf.CeilToInt(beats);
			return new BarAndBeat(beatsInt / beatsPerBar, beatsInt % beatsPerBar);
		}
		
		//operator for comparing
		#region OPERATORS 
		public static bool operator ==(BarAndBeat x, BarAndBeat y)
		{
			return x.Bar == y.Bar && x.Beat == y.Beat;
		}

		public static bool operator !=(BarAndBeat x, BarAndBeat y)
		{
			return x.Bar != y.Bar || x.Beat != y.Beat;
		}

		public static bool operator >(BarAndBeat x, BarAndBeat y)
		{
			if (x.Bar > y.Bar)
				return true;

			if (x.Bar == y.Bar)
				return x.Beat > y.Beat;

			return false;
		}

		public static bool operator <(BarAndBeat x, BarAndBeat y)
		{
			if (x.Bar < y.Bar)
				return true;

			if (x.Bar == y.Bar)
				return x.Beat < y.Beat;

			return false;
		}

		public override bool Equals(System.Object y)
		{
			return this == (BarAndBeat) y;
		}

		public static bool operator >=(BarAndBeat x, BarAndBeat y)
		{
			return x > y || x == y;
		}

		public static bool operator <=(BarAndBeat x, BarAndBeat y)
		{
			return x < y || x == y;
		}

		public override int GetHashCode()
		{
			return Bar ^ Beat;
		}
		#endregion

		#region VALUES		
		public static BarAndBeat Zero
		{
			get { return new BarAndBeat(0, 0); }
		}

		public static BarAndBeat OneBar
		{
			get { return new BarAndBeat(1, 0); }
		}

		public static BarAndBeat OneBeat
		{
			get { return new BarAndBeat(0, 1); }
		}
		#endregion
	}
}
