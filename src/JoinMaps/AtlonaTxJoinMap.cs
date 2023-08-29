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
	public class AtlonaTxJoinMap : AtlonaBaseJoinMap
	{
		#region Digital

        [JoinName("VideoSyncStatus")]
        public JoinDataComplete VideoSyncStatus = new JoinDataComplete(
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

        [JoinName("Input1VideoSyncStatus")]
        public JoinDataComplete Input1VideoSyncStatus = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 4,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Input 1 Video Has Sync",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("Input2VideoSyncStatus")]
        public JoinDataComplete Input2VideoSyncStatus = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 5,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Input 2 Video Has Sync",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("Input3VideoSyncStatus")]
        public JoinDataComplete Input3VideoSyncStatus = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 6,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Input 3 Video Has Sync",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });		

		#endregion


		


		#region Serial

		// TODO [ ] Add serial joins below plugin being developed

		public JoinDataComplete DeviceName = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 1,
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
        public AtlonaTxJoinMap(uint joinStart)
            : base(joinStart, typeof(AtlonaTxJoinMap))
		{
		}
	}
}