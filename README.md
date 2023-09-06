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