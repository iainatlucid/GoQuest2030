using Newtonsoft.Json;

namespace Lucid.GoQuest
{
	internal class Base
	{
		[JsonIgnore] internal int ID { get; set; }
		[JsonProperty] internal string Name { get; set; }
		public override string ToString() { return Name; }
	}
}
