using System.Collections.Generic;
using Crestron.SimplSharpPro;

namespace Lucid.GoQuest
{
	public class StringCache
	{
		private Dictionary<uint, string> cache;
		private DeviceStringInputCollection dsic;

		public StringCache(DeviceStringInputCollection d)
		{
			dsic = d;
			cache = new Dictionary<uint, string>();
		}
		public void SetString(uint key, string s)
		{
			string val;
			cache.TryGetValue(key, out val);
			if (val == null)
			{
				StdOut.WriteLine("Adding {0},{1}", key, s);
				dsic[key].StringValue = s;
				cache.Add(key, s);
			}
			else if (!s.Equals(val))
			{
				StdOut.WriteLine("Failed equality for {0} ({1}={2})", key, s, val);
				dsic[key].StringValue = s;
				cache[key] = s;
			}
		}
	}
	public class UShortCache
	{
		private Dictionary<uint, ushort?> cache;
		private DeviceUShortInputCollection duic;

		public UShortCache(DeviceUShortInputCollection d)
		{
			duic = d;
			cache = new Dictionary<uint, ushort?>();
		}
		public void SetUShort(uint key, ushort u, ushort range)
		{
			ushort? val;
			cache.TryGetValue(key, out val);
			if (val == null)
			{
				StdOut.WriteLine("Adding {0},{1}", key, u);
				duic[key].UShortValue = u;
				cache.Add(key, u);
			}
			else if ((u > val && ((u - val) > range)) || (u < val && ((val - u) > range)))
			{

				StdOut.WriteLine("Failed range for {0} ({1}={2})", key, u, val);
				duic[key].UShortValue = u;
				cache[key] = u;
			}
		}
	}
	public class BoolCache
	{
		private Dictionary<uint, bool?> cache;
		private DeviceBooleanInputCollection dbic;
		public BoolCache(DeviceBooleanInputCollection d)
		{
			dbic = d;
			cache = new Dictionary<uint, bool?>();
		}
		public void SetBool(uint key, bool b)
		{
			bool? val;
			cache.TryGetValue(key, out val);
			if (val == null)
			{
				StdOut.WriteLine("Adding {0},{1}", key, b);
				dbic[key].BoolValue = b;
				cache.Add(key, b);
			}
			else if (!b.Equals(val))
			{
				StdOut.WriteLine("Failed equality for {0} ({1}={2})", key, b, val);
				dbic[key].BoolValue = b;
				cache[key] = b;
			}
		}
	}
}