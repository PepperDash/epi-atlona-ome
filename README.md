# Essentials Atlona OME Plugin

## License

Provided under MIT license

## Overview

This plugin controls Atlona OME HDBase-T transmitter and receiver pairs, including sync detection and switching.

## Dependencies

The [Essentials](https://github.com/PepperDash/Essentials) libraries are required. They referenced via nuget. You must have nuget.exe installed and in the `PATH` environment variable to use the following command. Nuget.exe is available at [nuget.org](https://dist.nuget.org/win-x86-commandline/latest/nuget.exe).

### Installing Dependencies

Dependencies will be installed automatically by Visual Studio on opening. Use the Nuget Package Manager in
Visual Studio to manage nuget package dependencies. All files will be output to the `output` directory at the root of
repository.

### Installing Different versions of PepperDash Core

If a different version of PepperDash Core is needed, use the Visual Studio Nuget Package Manager to install the desired
version.

# Usage

## Join Maps

### Receiver Join Map

#### Digitals

| Join Number | Join Span | Description                    | Type                | Capabilities |
| ----------- | --------- | ------------------------------ | ------------------- | ------------ |
| 1        | 1         | Device Online                  | Digital             | ToSIMPL      |
| 2        | 1         | Any Input Has Sync                  | Digital             | ToSIMPL      |

#### Analogs

| Join Number | Join Span | Description               | Type                | Capabilities |
| ----------- | --------- | ------------------------- | ------------------- | ------------ |
| 1        | 1         | A/V Input Set/Get                | Analog              | ToFromSIMPL      |
| 3        | 1         | HDCP Support Capability  | Analog              | ToSIMPL      |
| 9        | 1         | HDCP Input Count        | Analog              | ToSIMPL      |

#### Serials

| Join Number | Join Span | Description           | Type                | Capabilities |
| ----------- | --------- | --------------------- | ------------------- | ------------ |
| 6       | 1         | Device Name | Serial              | ToSIMPL      |
| 2        | 1         | Mic Name              | Serial              | ToSIMPL      |
| 50        | 1         | Device Name Name      | Serial              | ToSIMPL      |

### Transmitter Join Map

#### Digitals

| Join Number | Join Span | Description                    | Type                | Capabilities |
| ----------- | --------- | ------------------------------ | ------------------- | ------------ |
| 1        | 1         | Device Online                  | Digital             | ToSIMPL      |
| 2        | 1         | Any Input Has Sync                  | Digital             | ToSIMPL      |
| 4        | 1         | Input 1 Has Sync                  | Digital             | ToSIMPL      |
| 5        | 1         | Input 2 Has Sync                  | Digital             | ToSIMPL      |
| 6        | 1         | Input 3 Has Sync                  | Digital             | ToSIMPL      |

#### Analogs

| Join Number | Join Span | Description               | Type                | Capabilities |
| ----------- | --------- | ------------------------- | ------------------- | ------------ |
| 1        | 1         | A/V Input Set/Get                | Analog              | ToFromSIMPL      |
| 3        | 1         | HDCP Support Capability  | Analog              | ToSIMPL      |
| 9        | 1         | HDCP Input Count        | Analog              | ToSIMPL      |

#### Serials

| Join Number | Join Span | Description           | Type                | Capabilities |
| ----------- | --------- | --------------------- | ------------------- | ------------ |
| 1       | 1         | Device Name | Serial              | ToSIMPL      |
| 6       | 1         | Device Name | Serial              | ToSIMPL      |
| 2        | 1         | Mic Name              | Serial              | ToSIMPL      |
| 50        | 1         | Device Name Name      | Serial              | ToSIMPL      |

## Example Config

### Receiver

#### Device Types

Valid device types are
* `AtOmeRx21`
* `AT-OME-RX21`
* `RX21`
* `OMERX21`

#### Control Methods


```json
{
  "key": "VRX-1",
  "name": "Laptop",
  "group": "api",
  "type": "AtOmeRx21",
  "properties": {
    "control": {
        "method": "tcpIp",
        "tcpSshProperties": {
            "address": "192.168.0.231",
            "port": "23",
            "autoReconnect": true,
            "autoReconnectIntervalMs": 5000
        }
    },
    "pollTimeMs" : 3000,
    "warningTimeoutMs" : 60000,
    "errorTimeoutMs" : 180000
  }
}
```

### Transmitter

#### Device Types

Valid device types are
* `AtOmeSt31A`,
* `AT-OME-ST31A`
* `ST31A`
* `OMEST31A`

#### Control Methods


```json
{
  "key": "VTX-1",
  "name": "Laptop",
  "group": "api",
  "type": "AtOmeSt31A",
  "properties": {
    "control": {
        "method": "tcpIp",
        "tcpSshProperties": {
            "address": "192.168.0.231",
            "port": "23",
            "autoReconnect": true,
            "autoReconnectIntervalMs": 5000
        }
    },
    "pollTimeMs" : 3000,
    "warningTimeoutMs" : 60000,
    "errorTimeoutMs" : 180000
  }
}
```
<!-- START Minimum Essentials Framework Versions -->
### Minimum Essentials Framework Versions

- 1.13.4
<!-- END Minimum Essentials Framework Versions -->
<!-- START Config Example -->
### Config Example

```json
{
    "key": "GeneratedKey",
    "uid": 1,
    "name": "GeneratedName",
    "type": "AtlonaOmeConfig",
    "group": "Group",
    "properties": {
        "control": "SampleValue",
        "pollTimeMs": 0,
        "warningTimeoutMs": 0,
        "errorTimeoutMs": 0,
        "deviceSerialNumber": "SampleString"
    }
}
```
<!-- END Config Example -->
<!-- START Supported Types -->

<!-- END Supported Types -->
<!-- START Join Maps -->
### Join Maps

#### Digitals

| Join | Type (RW) | Description |
| --- | --- | --- |
| 1 | R | Is Online |

#### Analogs

| Join | Type (RW) | Description |
| --- | --- | --- |
| 1 | R | A/V Input Set / Get |
| 3 | R | HDCP Support Setting Capabilities |
| 9 | R | Number of Ports with HDCP Setting Support |
<!-- END Join Maps -->
<!-- START Interfaces Implemented -->
### Interfaces Implemented

- IDeviceInfoProvider
- ICommunicationMonitor
- IHdBaseTInput1
- IHdBaseTInput2
- IHdmiInput3
- IRoutingFeedback
- IAtlonaRoutingPoll
- IHdmiInput2
- ITxRoutingWithFeedback
- IUsbCInput1
- IHasPowerControlWithFeedback
<!-- END Interfaces Implemented -->
<!-- START Base Classes -->
### Base Classes

- EssentialsBridgeableDevice
- AtlonaOmeDevice
- AtlonaBaseJoinMap
- JoinMapBaseAdvanced
<!-- END Base Classes -->
<!-- START Public Methods -->
### Public Methods

- public string GetMacFromArpTable(string ipaddress)
- public void ResolveHostData()
- public void SendText(string text)
- public void GetIpConfig()
- public void GetModel()
- public void GetFirmware()
- public void GetHostname()
- public void ReportDeviceInfo()
- public void UpdateDeviceInfo()
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
- public void ExecuteNumericSwitch(ushort input, ushort output, eRoutingSignalType type)
- public void PollRouteStatus()
- public void PollInputStatus()
- public void HdBaseTInput1()
- public void HdBaseTInput2()
- public void HdmiInput3()
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
- public void ExecuteNumericSwitch(ushort input, ushort output, eRoutingSignalType type)
- public void PollRouteStatus()
- public void PollInputStatus()
- public void HdBaseTInput1()
- public void HdmiInput2()
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
- public void ExecuteNumericSwitch(ushort input, ushort output, eRoutingSignalType type)
- public void UsbCInput1()
- public void HdmiInput2()
- public void PollRouteStatus()
- public void PollInputStatus()
- public void PollPower()
- public void PowerOff()
- public void PowerOn()
- public void PowerToggle()
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
- public void ExecuteNumericSwitch(ushort input, ushort output, eRoutingSignalType type)
- public void UsbCInput1()
- public void HdmiInput2()
- public void HdmiInput3()
- public void PollRouteStatus()
- public void PollInputStatus()
- public void PollPower()
- public void PowerOff()
- public void PowerOn()
- public void PowerToggle()
<!-- END Public Methods -->
<!-- START Bool Feedbacks -->
### Bool Feedbacks

- HdBaseTInput1SyncFeedback
- HdBaseTInput2SyncFeedback
- HdmiInput3SyncFeedback
- HdBaseTInput1SyncFeedback
- HdmiInput2SyncFeedback
- UsbCInput1SyncFeedback
- HdmiInput2SyncFeedback
- PowerIsOnFeedback
- UsbCInput1SyncFeedback
- HdmiInput2SyncFeedback
- HdmiInput3SyncFeedback
- PowerIsOnFeedback
<!-- END Bool Feedbacks -->
<!-- START Int Feedbacks -->
### Int Feedbacks

- StatusFeedback
- AudioVideoSourceNumericFeedback
- AudioVideoSourceNumericFeedback
- VideoSourceNumericFeedback
- AudioSourceNumericFeedback
- VideoSourceNumericFeedback
- AudioSourceNumericFeedback
<!-- END Int Feedbacks -->
<!-- START String Feedbacks -->
### String Feedbacks

- ModelFeedback
- MakeFeedback
<!-- END String Feedbacks -->
