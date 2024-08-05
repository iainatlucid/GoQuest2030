using Newtonsoft.Json;
using System;

namespace Lucid.GoQuest
{
	internal class Base
	{
		[JsonProperty] protected int id;
		[JsonProperty] protected string name;
		public override string ToString() { return name; }
	}
}
