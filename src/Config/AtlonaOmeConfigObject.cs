using Newtonsoft.Json;
using PepperDash.Essentials.Core;

namespace AtlonaOme.Config
{
	/// <summary>
	/// Plugin device configuration object
	/// </summary>
	/// <remarks>
	/// Rename the class to match the device plugin being created
	/// </remarks>
	/// <example>
	/// "EssentialsPluginConfigObjectTemplate" renamed to "SamsungMdcConfig"
	/// </example>
	[ConfigSnippet("\"properties\":{\"control\":{}")]
	public class AtlonaOmeConfigObject
	{
		[JsonProperty("control")]
		public EssentialsControlPropertiesConfig Control { get; set; }
		[JsonProperty("pollTimeMs")]
		public long PollTimeMs { get; set; }
		[JsonProperty("warningTimeoutMs")]
		public long WarningTimeoutMs { get; set; }
		[JsonProperty("errorTimeoutMs")]
		public long ErrorTimeoutMs { get; set; }
        [JsonProperty("deviceSerialNumber")]
        public string DeviceSerialNumber { get; set; }
	}
}