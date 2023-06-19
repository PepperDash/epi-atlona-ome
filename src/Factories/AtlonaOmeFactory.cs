using System;
using System.Collections.Generic;
using System.Linq;
using AtlonaOme.Devices.Transmitters;
using PepperDash.Core;
using AtlonaOme.Config;
using AtlonaOme.Devices;
using AtlonaOme.Devices.Receivers;
using PepperDash.Essentials.Core;

namespace AtlonaOme.Factories
{
	/// <summary>
	/// Plugin device factory for devices that use IBasicCommunication
	/// </summary>
	/// <remarks>
	/// Rename the class to match the device plugin being developed
	/// </remarks>
	/// <example>
	/// "EssentialsPluginFactoryTemplate" renamed to "MyDeviceFactory"
	/// </example>
    public class AtlonaOmeFactory : EssentialsPluginDeviceFactory<AtlonaOmeDevice>
    {
		/// <summary>
		/// Plugin device factory constructor
		/// </summary>
		/// <remarks>
		/// Update the MinimumEssentialsFrameworkVersion & TypeNames as needed when creating a plugin
		/// </remarks>
		/// <example>
 		/// Set the minimum Essentials Framework Version
		/// <code>
		/// MinimumEssentialsFrameworkVersion = "1.13.4;
        /// </code>
		/// In the constructor we initialize the list with the typenames that will build an instance of this device
        /// <code>
		/// TypeNames = new List<string/>() { "SamsungMdc", "SamsungMdcDisplay" };
        /// </code>
		/// </example>
        private readonly List<string> _omeRx21TypeNames = new List<string> { "AtOmeRx21", "AT-OME-RX21", "RX21", "OMERX21" };
        private readonly List<string> _omeSt31ATypeNames = new List<string> { "AtOmeSt31A", "AT-OME-ST31A", "ST31A", "OMEST31A" };

        public AtlonaOmeFactory()
        {
            MinimumEssentialsFrameworkVersion = "1.13.4";


            //If you add more devices, create a new list of strings for each device type, and then concat them like I did here - makes it easier to search for them.
            TypeNames = new List<string>().Concat(_omeRx21TypeNames).Concat(_omeSt31ATypeNames).ToList();

        }
        
		/// <summary>
		/// Builds and returns an instance of EssentialsPluginDeviceTemplate
		/// </summary>
		/// <param name="dc">device configuration</param>
		/// <returns>plugin device or null</returns>
		/// <remarks>		
		/// The example provided below takes the device key, name, properties config and the comms device created.
		/// Modify the EssetnialsPlugingDeviceTemplate constructor as needed to meet the requirements of the plugin device.
		/// </remarks>
		/// <seealso cref="PepperDash.Core.eControlMethod"/>
        public override EssentialsDevice BuildDevice(PepperDash.Essentials.Core.Config.DeviceConfig dc)
        {
            Debug.Console(1, "[{0}] Factory Attempting to create new device from type: {1}", dc.Key, dc.Type);

            // get the plugin device properties configuration object & check for null 
            var propertiesConfig = dc.Properties.ToObject<AtlonaOmeConfigObject>();
            if (propertiesConfig == null)
            {
                Debug.Console(0, "[{0}] Factory: failed to read properties config for {1}", dc.Key, dc.Name);
                return null;
            }

            var comms = CommFactory.CreateCommForDevice(dc);
            if (comms == null)
            {
                Debug.Console(1, "[{0}] Factory Notice: No control object present for device {1}", dc.Key, dc.Name);
                return null;
            }
            if (_omeRx21TypeNames.Contains(dc.Type, StringComparer.OrdinalIgnoreCase))
                return new AtlonaOmeRx21Device(dc.Key, dc.Name, propertiesConfig, comms);
            if (_omeSt31ATypeNames.Contains(dc.Type, StringComparer.OrdinalIgnoreCase))
                return new AtlonaOmeSt31ADevice(dc.Key, dc.Name, propertiesConfig, comms);

		    return null;

        }

    }
}

          