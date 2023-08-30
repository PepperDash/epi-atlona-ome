using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;

namespace AtlonaOme.JoinMaps
{
	public  class AtlonaBaseJoinMap:JoinMapBaseAdvanced

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
				JoinNumber = 3,
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
				JoinNumber = 9,
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

		public AtlonaBaseJoinMap(uint joinStart, Type type) : base(joinStart, type) { 
		}

		public AtlonaBaseJoinMap(uint joinStart) : this(joinStart, typeof(AtlonaBaseJoinMap)) { }
	}
}