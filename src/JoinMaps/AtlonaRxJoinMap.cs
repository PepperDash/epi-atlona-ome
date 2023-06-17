using PepperDash.Essentials.Core;

namespace AtlonaOme.JoinMaps
{
	/// <summary>
	/// Plugin device Bridge Join Map
	/// </summary>
	/// <remarks>
	/// Rename the class to match the device plugin being developed.  Reference Essentials JoinMaps, if one exists for the device plugin being developed
	/// </remarks>
	/// <see cref="PepperDash.Essentials.Core.Bridges"/>
	/// <example>
	/// "EssentialsPluginBridgeJoinMapTemplate" renamed to "SamsungMdcBridgeJoinMap"
	/// </example>
	public class AtlonaRxJoinMap : JoinMapBaseAdvanced
	{
		#region Digital


		[JoinName("IsOnline")]
		public JoinDataComplete IsOnline = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 1,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Is Online",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Digital
			});

        [JoinName("InputSync")]
        public JoinDataComplete InputSync = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 2,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Any Input Video Has Sync",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

		#endregion


		#region Analog

		// TODO [ ] Add analog joins below plugin being developed

        [JoinName("AudioVideoInput")]
        public JoinDataComplete AudioVideoInput = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "A/V Input Set / Get",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });
        [JoinName("HdcpSupportCapability")]
        public JoinDataComplete HdcpSupportCapability = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 2,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "HDCP Support Setting Capabilities",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });
        [JoinName("HdcpInputPortCount")]
        public JoinDataComplete HdcpInputPortCount = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 5,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Number of Ports with HDCP Setting Support",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });

		#endregion


		#region Serial

		// TODO [ ] Add serial joins below plugin being developed

		public JoinDataComplete DeviceName = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 6,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Device Name",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Serial
			});

		#endregion

		/// <summary>
		/// Plugin device BridgeJoinMap constructor
		/// </summary>
		/// <param name="joinStart">This will be the join it starts on the EISC bridge</param>
        public AtlonaRxJoinMap(uint joinStart)
            : base(joinStart, typeof(AtlonaRxJoinMap))
		{
		}
	}
}