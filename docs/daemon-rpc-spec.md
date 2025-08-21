
<!-- omit from toc -->
# The daemon RPC specification, v1

This document describes how to perform RPC related operations with the daemon.
This API is currently subject to changes.

<!-- omit from toc -->
# Table Of Contents

- [1. Terminology](#1-terminology)
  - [1.1. arrayOf(\<value\>)](#11-arrayofvalue)
  - [1.2. oneOf("\<value1\>"|"\<value2\>"|...)](#12-oneofvalue1value2)
- [2. Communication](#2-communication)
  - [2.1. Server](#21-server)
  - [2.2. Client](#22-client)
- [3. Protocol](#3-protocol)
  - [3.1. Message Format](#31-message-format)
  - [3.2. Message Types](#32-message-types)
    - [3.2.1. Client](#321-client)
    - [3.2.2. Server](#322-server)
  - [3.3. Messages specification](#33-messages-specification)
    - [3.3.1. Requests](#331-requests)
    - [3.3.2. Responses](#332-responses)
      - [3.3.2.1. OK](#3321-ok)
      - [3.3.2.2. Error](#3322-error)
      - [3.3.2.3. Platform Information](#3323-platform-information)
      - [3.3.2.4. Version](#3324-version)
      - [3.3.2.5. Features](#3325-features)
      - [3.3.2.6. Adapter](#3326-adapter)
      - [3.3.2.7. Adapters](#3327-adapters)
      - [3.3.2.8. Device](#3328-device)
      - [3.3.2.9. Paired Devices](#3329-paired-devices)
      - [3.3.2.10. File transfer](#33210-file-transfer)
    - [3.3.3. Events](#333-events)
      - [3.3.3.1. Error Event](#3331-error-event)
      - [3.3.3.2. Adapter Event](#3332-adapter-event)
      - [3.3.3.3. Device Event](#3333-device-event)
      - [3.3.3.4. File Transfer Event](#3334-file-transfer-event)
      - [3.3.3.5. Pairing Authentication Event](#3335-pairing-authentication-event)
      - [3.3.3.6. File Transfer Authentication Event](#3336-file-transfer-authentication-event)
- [3.4. Commands](#34-commands)
  - [3.4.0.1. Method Invocation](#3401-method-invocation)
  - [3.4.0.2. Command Format](#3402-command-format)
  - [3.5. Command List](#35-command-list)
    - [3.5.1. Rpc Session Command Section](#351-rpc-session-command-section)
      - [3.5.1.1. rpc platform-info](#3511-rpc-platform-info)
      - [3.5.1.2. rpc feature-flags](#3512-rpc-feature-flags)
      - [3.5.1.3. rpc version](#3513-rpc-version)
      - [3.5.1.4. rpc auth](#3514-rpc-auth)
    - [3.5.2. Adapter Commands Section](#352-adapter-commands-section)
      - [3.5.2.1. adapter list](#3521-adapter-list)
      - [3.5.2.2. adapter properties](#3522-adapter-properties)
      - [3.5.2.3. adapter discovery start](#3523-adapter-discovery-start)
      - [3.5.2.4. adapter discovery stop](#3524-adapter-discovery-stop)
      - [3.5.2.5. adapter get-paired-devices](#3525-adapter-get-paired-devices)
      - [3.5.2.6. adapter set-powered-state](#3526-adapter-set-powered-state)
      - [3.5.2.7. adapter set-pairable-state](#3527-adapter-set-pairable-state)
      - [3.5.2.8. adapter set-discoverable-state](#3528-adapter-set-discoverable-state)
    - [3.5.3. Device Commands Section](#353-device-commands-section)
      - [3.5.3.1. device properties](#3531-device-properties)
      - [3.5.3.2. device pair](#3532-device-pair)
      - [3.5.3.3. device pair cancel](#3533-device-pair-cancel)
      - [3.5.3.4. device connect](#3534-device-connect)
      - [3.5.3.5. device connect profile](#3535-device-connect-profile)
      - [3.5.3.6. device disconnect](#3536-device-disconnect)
      - [3.5.3.7. device remove](#3537-device-remove)
      - [3.5.3.8. device opp start-session](#3538-device-opp-start-session)
      - [3.5.3.9. device opp send-file](#3539-device-opp-send-file)
      - [3.5.3.10. device opp cancel-transfer](#35310-device-opp-cancel-transfer)
      - [3.5.3.11. device opp suspend-transfer](#35311-device-opp-suspend-transfer)
      - [3.5.3.12. device opp resume-transfer](#35312-device-opp-resume-transfer)
      - [3.5.3.13. device opp stop-session](#35313-device-opp-stop-session)
      - [3.5.3.14. device opp start-server](#35314-device-opp-start-server)
      - [3.5.3.15. device opp stop-server](#35315-device-opp-stop-server)


# 1. Terminology
Within this specification, certain terminologies are used to specify the type
of value or a collection of values.

## 1.1. arrayOf(\<value\>)
This means that the property is an array or a collection of the specified `<value>`.
For example, in this sample JSON specification:
```javascript
{
"intValues": arrayOf(int) 
}
```

In the above specification, the "intValues" property accepts an array of integers.
A JSON message matching the above specification would look like:
```javascript
{
"intValues": [1, 2, 3]
}
```

## 1.2. oneOf("\<value1\>"|"\<value2\>"|...)
This means that the property must accept only one of the specified values.
For example, in this sample JSON specification:
```javascript
{
"status": oneOf("ok"|"pending"|"error")
}
```

In the above specification, the "status" property accepts only one of the values "ok", "pending", or "error".
A JSON message matching the above specification would look like:
```javascript
{
"status": "ok"
}
```

# 2. Communication
First, we'll establish the method for a RPC server and client to find and
connect to each other.

The communication between a server and client is done via **UNIX sockets** only,
since it is a system related service.

## 2.1. Server
- The server must be started first (i.e. using the `server start` command, see [Commands](#34-commands)). All services related to the [features](#feature-flags) it advertises must be started first.

- Then, the server must use the following well-defined paths to create the socket according to the operating system in use:
	
	
| OS                                | Full path to socket                                                                                                   |
| --------------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| Windows                           | `%LocalAppData%\haraltd\hd.sock`                                                                                 |
| MacOS                             | `$HOME/Library/Caches/haraltd/hd.sock`                                                                           |
| Other UNIX like operating systems | If **$XDG_CACHE_HOME** is defined, ` $XDG_CACHE_HOME/haraltd/hd.sock`, else ` $HOME/.cache/haraltd/hd.sock` |

- After creating the socket, the server must start listening and accept any client connections. Currently, no authentication is implemented between a server and client, therefore clients can connect and send commands immediately.

## 2.2. Client

Using the well defined paths above, clients can discover the running server according to the operating system they are running on.

Clients are currently allowed to send defined [commands](#34-commands) directly
without any authentication.

# 3. Protocol
Next, we'll define the protocol used to send messages and invoke commands from a client to the server.

## 3.1. Message Format
All messages are in the [JSON Lines](jsonlines.org) format.

## 3.2. Message Types

### 3.2.1. Client
A client can send only [requests](#331-requests).

The complete request must be serialized to bytes before sending it to the server.

### 3.2.2. Server
A server can send only [responses](#332-responses) and [events](#333-events).

The complete response or event must be serialized to bytes before sending it to the client.

Responses are **final**: no request will receive more than one final response.

Events are fire-and-forget, and are not tracked by the server. Clients are not expected
to track events, but to consume them and update their internal states as required.

## 3.3. Messages specification
### 3.3.1. Requests

The complete request format is as follows:
```javascript
{
"request_id": int64,
"command": arrayOf(string)
}
```

- Where:
  - The **request_id** parameter is optional, but if provided, 
  must be a non-zero positive integer,  and must be embedded into both 
  the request from the client and the response from the server. The default value is 0.
  - The **command** parameter is required, and must be a non-empty array of strings.
  See  [Commands](#34-commands) for more details.

- Example JSON data
```javascript
{
"request_id": 10,
"command": ["device", "properties"]
}
```

### 3.3.2. Responses

The complete response format is as follows:

```javascript
{
"status": oneOf("ok"|"error"),
"operation_id": uint32,
"request_id": int64,
<response_property>: {...}
}
```

Where:
- #### status
**Required**. The **status** and the **response** properties must be mapped according to the following table:
| Status  | Response                                   |
| ------- | ------------------------------------------ |
| "ok"    | All other responses except [Error](#3322-error) |
| "error" | [Error](#3322-error)                            |

 - #### operation_id
**Required**. The **operation_id** property is a non-zero positive integer, that is uniquely generated by the server, and can be used by clients to track the request as well.

 - #### request_id
**Required**. The **request_id** property is a positive integer which tracks the request sent by the client. The server must embed the same request ID in the response that the client provided while sending its request, or if it wasn't provided, must be set to 0.

- #### <response_property>
**Required**. See the following types of **response** properties.

---

#### 3.3.2.1. OK
An operation that has run without errors may only wish to communicate that it
has run successfully, and would not want to send any other information.
Therefore, it sends an OK response.

The OK response format is:
```javascript
{
"status": "ok",
"operation_id": uint32,
"request_id": int64,
}
```

`status`
: **Required**. See [status](#status).

`operation_id`
: **Required**. See [operation_id](#operation_id).

`request_id`
: **Required**. See [request_id](#request_id).

---

#### 3.3.2.2. Error
An error is sent, if:

- The request was not parsed correctly
- The command was not parsed correctly/not found
- The invoked operation was executed, but the operation returned an error.

As mentioned in the [responses](#332-responses) section, the error format is:

```javascript
{
"status": "error",
"operation_id": uint32,
"request_id": int64,
"error":  {
		"code": int,
		"name": string,
		"description": string,
		"metatdata": {
		  "object1": string,
		  "object2": string,
		  ...
		  "objectN": string,
		},
	}
}
```

`status`
: **Required**. See [status](#status).

`operation_id`
: **Required**. See [operation_id](#operation_id).

`request_id`
: **Required**. See [request_id](#request_id).

`code`
:  **Required.** If any errors occurred during the processing of the request, this property must be a non-zero value.

`name`
:  **Required.** The name of the error.

`description`
: **Required.** A brief description of the error.

`metadata`
: **Optional.** A key-value like property providing additional information about the error.

- Example JSON data
```javascript
{
"status": "ok",
"operation_id": 2,
"request_id": 100,
"error":  {
		"code": -2500,
		"name": "ERROR_JSON_REQUEST_PARSE",
		"description": "An error occurred while parsing the JSON request",
		"metatdata": {
		  "exception": "Could not parse token at position 1",
		  "additional": "Request processing cancelled",
		},
	}
}
```
---

#### 3.3.2.3. Platform Information
This type of response represents the platform information of the server.

_Format:_
```javascript
{
"status": "ok",
"operation_id": uint32,
"request_id": int64,
"data": {
    "platform_info": {
      "stack": string,
      "os_info": string
    }
  }
}
```

`status`
: **Required**. See [status](#status).

`operation_id`
: **Required**. See [operation_id](#operation_id).

`request_id`
: **Required**. See [request_id](#request_id).

`stack`
: **Required**. The name of the Bluetooth stack.

`os_info`
: **Required**. The general Operating System and architecture information.

- Example JSON data
```javascript
{
"status": "ok",
"operation_id": 2,
"request_id": 100,
"data": {
    "platform_info": {
      "stack": "BlueZ",
      "os_info": "Linux amd64"
    }
  }
}
```

---

#### 3.3.2.4. Version
This type of response represents the version of a server.

_Format:_
```javascript
{
"status": "ok",
"operation_id": uint32,
"request_id": int64,
"data": {
    "version": string
  }
}
```

`status`
: **Required**. See [status](#status).

`operation_id`
: **Required**. See [operation_id](#operation_id).

`request_id`
: **Required**. See [request_id](#request_id).

`version`
: **Required**. The version of the server instance.

- Example JSON data
```javascript
{
"status": "ok",
"operation_id": 2,
"request_id": 100,
"data": {
    "version": "v0.0.3"
  }
}
```

---

#### 3.3.2.5. Features
This type of response represents the features or capabilities of a server.

_Format:_
```javascript
{
"status": "ok",
"operation_id": uint32,
"request_id": int64,
"data": {
    "features": int
  }
}
```

`status`
: **Required**. See [status](#status).

`operation_id`
: **Required**. See [operation_id](#operation_id).

`request_id`
: **Required**. See [request_id](#request_id).

`features`:
**Required**. The **features** property is a combination of one or more features or capabilities of the server, as listed in this table:
| Feature           | Value  |
| ----------------- | ------ |
| Connection        | 1 << 1 |
| Pairing           | 1 << 2 |
| Send File         | 1 << 3 |
| Receive file      | 1 << 4 |
| Network Tethering | 1 << 5 |
| Media Control     | 1 << 6 |

For example, if the server wants to advertise that it can pair and connect to devices, and send and receive files, it would combine each feature like so:
```go
const (
  Connection uint = 1 << 1
  Pairing uint = 1 << 2
  SendFile uint = 1 << 3
  ReceiveFile uint = 1 << 4
)

var features = Connection | Pairing | SendFile | ReceiveFile // features = 30
```

- Example JSON data
```javascript
{
"status": "ok",
"operation_id": 2,
"request_id": 100,
"data": {
    "features": 30
  }
}
```

---

#### 3.3.2.6. Adapter
This type of response respresents the full properties of an adapter (i.e. when the [`adapter properties`](#3326-adapter-properties) method is called).

_Format:_
```javascript
{
"status": "ok",
"operation_id": uint32,
"request_id": int64,
"data": {
    "adapter": {
      "address": string,
      "powered": bool,
      "discoverable": bool,
      "pairable": bool,
      "discovering": bool,
      "name": string,
      "alias": string,
      "unique_name": string,
      "uuids": arrayOf(UUID)
    }
  }
}
```

`status`
: **Required**. See [status](#status).

`operation_id`
: **Required**. See [operation_id](#operation_id).

`request_id`
: **Required**. See [request_id](#request_id).

`address`
: **Required.** The address of the adapter.

`powered`
: **Required.** Indicates whether the adapter is powered.

`discoverable`
: **Required.** Indicates whether the adapter is discoverable.

`pairable`
: **Required.** Indicates whether the adapter is pairable.

`discovering`
: **Optional.** Indicates whether the adapter is currently discovering devices.

`name`
: **Required.** The name of the adapter.

`alias`
: **Required.** The alias of the adapter. Only valid on Linux, will equate to `name` on other systems.

`unique_name`
: **Required.** The unique name of the adapter. Only valid on Linux, will equate to `name` on other systems.

`uuids`
: **Required.** The UUIDs of the services supported by the adapter.


- Example JSON data

```javascript
{
"status": "ok",
"operation_id": 2,
"request_id": 100,
"data": {
    "adapter": {
      "address": "00:1A:7D:DA:71:13",
      "powered": true,
      "discoverable": false,
      "pairable": true,
      "discovering": false,
      "name": "Adapter",
      "alias": "Adapter",
      "unique_name": "hci0",
      "uuids": [
        "0000110d-0000-1000-8000-00805f9b34fb",
        "0000111e-0000-1000-8000-00805f9b34fb"
      ]
    }
  }
}
```

---

#### 3.3.2.7. Adapters
This type of response represents a list of adapters.

_Format_:
```javascript
{
"status": "ok",
"operation_id": uint32,
"request_id": int64,
"data": {
    "adapters": arrayOf(Adapter)
  }
}
```

`status`
: **Required**. See [status](#status).

`operation_id`
: **Required**. See [operation_id](#operation_id).

`request_id`
: **Required**. See [request_id](#request_id).

`adapters`
: **Required**. A list of [adapter](#3326-adapter) objects.

- Example JSON data
```javascript
{
"status": "ok",
"operation_id": 2,
"request_id": 100,
"data": {
    "adapters": [
        {
        "address": "00:1A:7D:DA:71:13",
        "powered": true,
        "discoverable": false,
        "pairable": true,
        "discovering": false,
        "name": "Adapter",
        "alias": "Adapter",
        "unique_name": "hci0",
        "uuids": [
          "0000110d-0000-1000-8000-00805f9b34fb",
          "0000111e-0000-1000-8000-00805f9b34fb"
        ]
      },
      {
        "address": "00:AA:BB:CC:DD:EE",
        "powered": true,
        "discoverable": false,
        "pairable": true,
        "discovering": false,
        "name": "Adapter1",
        "alias": "Adapter1",
        "unique_name": "hci1",
        "uuids": [
          "0000110d-0000-1000-8000-00805f9b34fb",
          "0000111e-0000-1000-8000-00805f9b34fb"
        ]
      },
    ]
  }
}
```

---

#### 3.3.2.8. Device
This type of response respresents the full properties of a device (i.e. when the [`device properties`](#3328-device-properties) method is called).

_Format:_
```javascript
{
"status": "ok",
"operation_id": uint32,
"request_id": int64,
"data": {
    "device": {
      "name": string,
      "alias": string,
      "class": uint,
      "legacy_pairing": bool,
      "address": string,
      "connected": bool,
      "paired": bool,
      "blocked": bool,
      "bonded": bool,
      "rssi": short,
      "percentage": int,
      "uuids": arrayOf(UUID)
    }
  }
}
```

`status`
: **Required**. See [status](#status).

`operation_id`
: **Required**. See [operation_id](#operation_id).

`request_id`
: **Required**. See [request_id](#request_id).

`name`
: **Required.** The name of the device.

`alias`
: **Required.** The alias of the device. Valid only on Linux systems, can equate to `name` on other systems.

`class`
: **Required.** The class of the device.

`legacy_pairing`
: **Required.** Indicates whether the device uses legacy pairing.

`address`
: **Required.** The address of the device.

`connected`
: **Required.** Indicates whether the device is connected.

`paired`
: **Required.** Indicates whether the device is paired.

`blocked`
: **Required.** Indicates whether the device is blocked.

`bonded`
: **Required.** Indicates whether the device is bonded.

`uuids`
: **Required.** The UUIDs of the services supported by the device.

`rssi`
: **Optional.** The RSSI (Received Signal Strength Indicator) of the device.

`percentage`
: **Optional.** The battery percentage of the device.

- Example JSON data

```javascript
{
"status": "ok",
"operation_id": 2,
"request_id": 100,
"data": {
  "device": {
      "address": "00:1A:7D:DA:71:13",
      "connected": true,
      "paired": true,
      "blocked": false,
      "bonded": true,
      "rssi": -45,
      "percentage": 85,
      "uuids": [
        "0000110d-0000-1000-8000-00805f9b34fb",
        "0000111e-0000-1000-8000-00805f9b34fb"
      ],
      "name": "Bluetooth Device",
      "alias": "Device1",
      "class": 1,
      "legacy_pairing": false
    }
  }
}
```

---

#### 3.3.2.9. Paired Devices
This type of response represents a list of paired devices.

_Format:_
```javascript
{
"status": "ok",
"operation_id": uint32,
"request_id": int64,
"data": {
    "paired_devices": arrayOf(Device)
  }
}
```

`status`
: **Required**. See [status](#status).

`operation_id`
: **Required**. See [operation_id](#operation_id).

`request_id`
: **Required**. See [request_id](#request_id).

`paired_devices`
: **Required**. A list of [device](#3328-device) objects. Note that for each of the devices, the **paired** and **bonded** properties must be set to **true**.

- Example JSON data

```javascript
{
"status": "ok",
"operation_id": 2,
"request_id": 100,
"data": {
  "paired_devices": [
      {
        "address": "00:1A:7D:DA:71:13",
        "connected": true,
        "paired": true,
        "blocked": false,
        "bonded": true,
        "rssi": -45,
        "percentage": 85,
        "uuids": [
          "0000110d-0000-1000-8000-00805f9b34fb",
          "0000111e-0000-1000-8000-00805f9b34fb"
        ],
        "name": "Bluetooth Device",
        "alias": "Device",
        "class": 1,
        "legacy_pairing": false
      },
      {
        "address": "00:AA:BB:CC:DD:EE",
        "connected": false,
        "paired": true,
        "blocked": false,
        "bonded": true,
        "uuids": [
          "0000110d-0000-1000-8000-00805f9b34fb",
          "0000111e-0000-1000-8000-00805f9b34fb"
        ],
        "name": "Bluetooth Device1",
        "alias": "Device1",
        "class": 1,
        "legacy_pairing": false
      }
    ]
  }
}
```

---

#### 3.3.2.10. File transfer
This type of response alone is usually only sent by the [`device opp send-file`](#3328-device-opp-send-file) command to indicate that a file transfer is queued for sending.

_Format:_
```javascript
{
"status": "ok",
"operation_id": uint32,
"request_id": int64,
"data": {
  "file_transfer": {
      "name": string,
      "address": string,
      "filename": string,
      "size": long,
      "transferred": long,
      "status": "queued"
    }
  }
}
```

`status`
: **Required**. See [status](#status).

`operation_id`
: **Required**. See [operation_id](#operation_id).

`request_id`
: **Required**. See [request_id](#request_id).

`name`
: **Required.** The name of the file.

`address`
: **Required.** The address associated with the file transfer event.

`filename`
: **Required.** The full pathname of the file being transferred.

`size`
: **Required.** The size of the file in bytes.

`transferred`
: **Required.** The number of bytes transferred so far.

`status`
: **Required.** The status of the file transfer.

- Example JSON data

```javascript
{
"status": "ok",
"operation_id": 2,
"request_id": 100,
"data": {
    "file_transfer": {
      "name": "example.txt",
      "address": "00:1A:7D:DA:71:13",
      "filename": "/tmp/example.txt",
      "size": 1024,
      "transferred": 512,
      "status": "queued"
    }
  }
}
```


### 3.3.3. Events
The complete event format is as follows:

```javascript
{
"event_id": oneOf(1|2|3|4|5|6),
"event_action": oneOf("added"|"updated"|"removed"),
"event": {
	<event_property>: { ... }
	}
}
```

Where:

- #### event_id
**Required**. The **event_id** and the **event** property must be mapped according to this table:
| Event ID | Event Property                                                                                                                                      |
| -------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1        | [Error Event](#3322-error-event)                                                                                                                         |
| 2        | [Adapter Event](#3326-adapter-event)                                                                                                                     |
| 3        | [Device Event](#3328-device-event)                                                                                                                       |
| 4        | [File Transfer Event](#33210-file-transfer-event)                                                                                                         |
| 6        | Either of [Pairing Authentication Event](#3335-pairing-authentication-event) or [File Transfer Authentication Event](file-transfer-authentication-event) |


- #### event_action
**Required**. The **event_action** property values and their descriptions are:

| Event Action | Description                                                                                                                                                       |
| ------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| added        | Indicates that an object was newly created within the system. For example, a new adapter or device could be added to the system.                                  |
| updated      | Indicates that an existing object's properties were changed or updated. For example, an adapter's discoverable state was changed.                                 |
| removed      | Indicates that an existing object was entirely removed from the system, and no longer exists. For example, an adapter or device could be removed from the system. |

- #### <event_property>
**Required**. See the following types of **event** properties.

---

#### 3.3.3.1. Error Event
This type of event is sent when any long running background operation encounters errors.

_Format:_
```javascript
{
  "event_id": 1,
  "event_action": "added",
  "event": {
    "error_event": {
      "code": string,
      "description": string,
    }
  }
}
```

`event_id`
: **Required**. See [event_id](#event_id).

`event_action`
: **Required**. See [event_action](#event_action).

`code`
: **Required.** The error code of the error event.

`description`
: **Required.** The description of the error event.

- Example JSON data

```javascript
{
  "event_id": 1,
  "event_action": "added",
  "event": {
    "error_event": {
      "code": "ERROR_UNEXPECTED",
      "description": "An unexpected error occurred",
    }
  }
}
```

---

#### 3.3.3.2. Adapter Event
This type of event is sent when an adapter is added, updated or removed from the system.

_Format:_
```javascript
{
  "event_id": 2,
  "event_action": oneOf("added"|"updated"|"removed"),
  "event": {
    "adapter_event": {
      "address": string,
      "powered": bool,
      "discoverable": bool,
      "pairable": bool,
      "discovering": bool
    }
  }
}
```

`event_id`
: **Required**. See [event_id](#event_id).

`event_action`
: **Required**. See [event_action](#event_action).

`address`
: **Required.** The address of the adapter.

`powered`
: **Optional.** Indicates whether the adapter is powered.

`discoverable`
: **Optional.** Indicates whether the adapter is discoverable.

`pairable`
: **Optional.** Indicates whether the adapter is pairable.

`discovering`
: **Optional.** Indicates whether the adapter is currently discovering devices.

- Example JSON data

```javascript
{
  "event_id": 2,
  "event_action": "added",
  "event": {
    "adapter_event": {
      "address": "00:1A:7D:DA:71:13",
      "powered": true,
      "pairable": true
    }
  }
}
```

---

#### 3.3.3.3. Device Event
This type of event is sent when a device is added, updated or removed from the system.

_Format:_
```javascript
{
  "event_id": 3,
  "event_action": oneOf("added"|"updated"|"removed"),
  "event": {
    "device_event": {
      "address": string,
      "connected": bool,
      "paired": bool,
      "blocked": bool,
      "bonded": bool,
      "rssi": short,
      "percentage": int,
      "uuids": arrayOf(UUID)
    }
  }
}
```

`event_id`
: **Required**. See [event_id](#event_id).

`event_action`
: **Required**. See [event_action](#event_action).

`address`
: **Required.** The address of the device.

`connected`
: **Optional.** Indicates whether the device is connected.

`paired`
: **Optional.** Indicates whether the device is paired.

`blocked`
: **Optional.** Indicates whether the device is blocked.

`bonded`
: **Optional.** Indicates whether the device is bonded.

`rssi`
: **Optional.** The RSSI (Received Signal Strength Indicator) of the device.

`percentage`
: **Optional.** The battery percentage of the device.

`uuids`
: **Optional.** The UUIDs of the services supported by the device.

- Example JSON data

```javascript
{
  "event_id": 3,
  "event_action": "updated",
  "event": {
    "device_event": {
      "address": "00:1A:7D:DA:71:13",
      "rssi": -45,
      "percentage": 85,
    }
  }
}
```

---

#### 3.3.3.4. File Transfer Event
This type of event is sent during a file transfer, either when initiated by the client, or when the server receives a file from the remote host.

_Format:_
```javascript
{
  "event_id": 4,
  "event_action": "updated",
  "event": {
    "file_transfer_event": {
      "name": string,
      "address": string,
      "filename": string,
      "size": long,
      "transferred": long,
      "status": oneOf("queued"|"active"|"suspended"|"complete"|"error")
    }
  }
}
```

`event_id`
: **Required**. See [event_id](#event_id).

`event_action`
: **Required**. See [event_action](#event_action).

`name`
: **Required.** The name of the file.

`address`
: **Required.** The address associated with the file transfer event.

`filename`
: **Required.** The full pathname of the file being transferred.

`size`
: **Required.** The size of the file in bytes.

`transferred`
: **Required.** The number of bytes transferred so far.

`status`
: **Required.** The status of the file transfer.

- Example JSON data

```javascript
{
  "event_id": 4,
  "event_action": "updated",
  "event": {
    "file_transfer_event": {
    "name": "example.txt",
    "address": "00:1A:7D:DA:71:13",
    "filename": "/tmp/example.txt",
    "size": 1024,
    "transferred": 512,
    "status": "active"
    }
  }
}
```

---

#### 3.3.3.5. Pairing Authentication Event

_Format:_
```javascript
{
  "event_id": 6,
  "event_action": "added",
  "event": {
    "pairing_auth_event": {
      "auth_id": int,
      "auth_event": oneOf("display-pincode"|"display-passkey"|"confirm-passkey"|"authorize-pairing"|"authorize-service"),
      "auth_reply_method": "reply-yes-no",
      "timeout_ms": int,
      "pincode": string,
      "passkey": int,
      "uuid": UUID
    }
  }
}
```

`event_id`
: **Required**. See [event_id](#event_id).

`event_action`
: **Required**. See [event_action](#event_action).

`auth_id`
: **Required.**. The authentication ID that is generated by the server and provided to clients to respond to this authentication event.

`timeout_ms`
: **Required**. The timeout of the authentication in milliseconds.

`auth_event`
: **Required**. The type of pairing method that is used to pair the device.
| `auth_event`          | `pincode` property required | `passkey` property required | `uuid` property required | Description                                                                                                                                        |
| --------------------- | --------------------------- | --------------------------- | ------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------- |
| **display-pincode**   | Yes                         | No                          | No                       | Used in legacy pairing scenarios, where the system needs to only display a pincode for authentication. For example, "0000" or "1234".              |
| **display-passkey**   | No                          | Yes                         | No                       | Used when the system needs to display a passkey for authentication. For example, 123456.                                                           |
| **confirm-passkey**   | No                          | Yes                         | No                       | Used when the system needs to ask the user for confirmation that the passkeys match on both the host and remote device.                            |
| **authorize-pairing** | No                          | No                          | No                       | Used when the system only needs the user to authorize the pairing process. No other pincodes or passkeys are involved.                             |
| **authorize-service** | No                          | No                          | Yes                      | Used when the system needs to authorize a specific service that the remote device advertises. The UUID value is the UUID of the Bluetooth profile. |


- Example JSON data

```javascript
{
  "event_id": 6,
  "event_action": "added",
  "event": {
    "pairing_auth_event": {
      "auth_id": 2500,
      "auth_event": "confirm-passkey",
      "auth_reply_method": "reply-yes-no",
      "timeout_ms": 10000,
      "passkey": 123456
    }
  }
}
```

```javascript
{
  "event_id": 6,
  "event_action": "added",
  "event": {
    "pairing_auth_event": {
      "auth_id": 2500,
      "auth_event": "display-pincode",
      "auth_reply_method": "reply-yes-no",
      "timeout_ms": 10000,
      "pincode": 1234
    }
  }
}
```

```javascript
{
  "event_id": 6,
  "event_action": "added",
  "event": {
    "pairing_auth_event": {
      "auth_id": 2500,
      "auth_event": "authorize-service",
      "auth_reply_method": "reply-yes-no",
      "timeout_ms": 10000,
      "uuid": "0000110d-0000-1000-8000-00805f9b34fb"
    }
  }
}
```

---

#### 3.3.3.6. File Transfer Authentication Event
This type of event is sent when a file is about to be received from a remote device, and the host or user needs to authorize the transfer.

_Format:_
```javascript
{
  "event_id": 6,
  "event_action": "added",
  "event": {
      {
        "transfer_auth_event": {
        "auth_id": int,
        "auth_event": "authorize-transfer",
        "auth_reply_method": "reply-yes-no",
        "timeout_ms": int,
        "file_transfer": {
          "name": string,
          "address": string,
          "filename": string,
          "size": long,
          "transferred": long,
          "status": "queued"
        }
      }
    }
  }
}
```

`event_id`
: **Required**. See [event_id](#event_id).

`event_action`
: **Required**. See [event_action](#event_action).

`auth_id`
: **Required.**. The authentication ID that is generated by the server and provided to clients to respond to this authentication event.

`timeout_ms`
: **Required**. The timeout of the authentication in milliseconds.

`file_transfer`
: **Required**. The complete information of the file that is about to be received. See [File Transfer](#33210-file-transfer) for more information on the properties.


- Example JSON data

```javascript
{
  "event_id": 6,
  "event_action": "added",
  "event": {
      "transfer_auth_event": {
      "auth_id": 2500,
      "auth_event": "authorize-transfer",
      "auth_reply_method": "reply-yes-no",
      "timeout_ms": 10000
      "file_transfer": {
        "name": "example.txt",
        "address": "00:1A:7D:DA:71:13",
        "filename": "/tmp/example.txt",
        "size": 1024,
        "transferred": 512,
        "status": "queued"
      }
    }
  }
}
```

# 3.4. Commands

## 3.4.0.1. Method Invocation

A client can send (multiple) requests in any order, and the server may not send the responses in the same order.
Therefore, clients are advised to embed a unique request ID within the request to track it, see the [requests](#331-requests) section.

The default timeout to receive a response for an request within a client is 30 seconds,
so a response sent by a server should be within that timeframe, or else the client will assume that the server experienced an internal error, and stop tracking the request.
The response sent after this timeframe is not guaranteed to be parsed by the client.

Within the server, all long running methods (for example, device discovery, file transfer etc.) must:
- Use [events](#333-events) to send updated information to the client.
- Run the operation in the background, and return a response that reflects the state of the operation immediately.
- If any errors occurs during the operation well after returning the response to the client, an [Error Event](#3322-error-event) must be sent reflecting the state of the operation.

The server may impose limits on the amount of concurrency and may stop reading from the client when server buffers are full. It is the client's responsibility to avoid concurrent-writing-induced deadlocks.

## 3.4.0.2. Command Format
As mentioned in the section on [requests](#331-requests), the format is:
```javascript
{
"request_id": int64,
"command": arrayOf(string)
}
```

The command property format is as follows:
```
"command": ["<method>", "<argument>", "<option1>", "<value1>", ..., "<optionN>", "<valueN>"]
```

For example, if a client wants to get the properties of a particular device, it would send a request like so:
```javascript
{
"request_id": 100,
"command": ["device", "properties", "--address", "AA:BB:CC:DD:EE:FF"]
}
```

Or if a client wants to respond to an authentication event, it would send a request like so:
```javascript
{
"request_id": 200,
"command": ["rpc", "auth", "--authentication-id", "10", "--response", "yes"]
}
```

## 3.5. Command List
The following set of commands can be used by clients to invoke various methods on the server.

### 3.5.1. Rpc Session Command Section

Perform functions related to an ongoing RPC operation within a session.

#### 3.5.1.1. rpc platform-info

Get the platform-specific information of the Bluetooth stack and the Operating System the server is running on.

Options: None

_Response values:_

- If the operation has run _Successfully_, the JSON response is: [Platform Information](#3323-platform-information).

- If the operation returns an _Error_, the JSON response is: [Error](#3322-error).

#### 3.5.1.2. rpc feature-flags

Show the features of the RPC server.

Options: None

_Response values:_

- If the operation has run _Successfully_, the JSON response is: [Feature Flags](#feature-flags).

- If the operation returns an _Error_, the JSON response is: [Error](#3322-error).

#### 3.5.1.3. rpc version

Show the current version information of the RPC server.

Options: None

_Response values:_

- If the operation has run _Successfully_, the JSON response is: [Version](#3324-version).

- If the operation returns an _Error_, the JSON response is: [Error](#3322-error).

#### 3.5.1.4. rpc auth

Set the response for a pending authentication request attached to an authentication ID.

| Option              | Required | Default Value | Description                                         |
| ------------------- | -------- | ------------- | --------------------------------------------------- |
| --response          | True     | N/A           | The response to sent to the authentication request. |
| --authentication-id | True     | N/A           | The ID of the authentication request.               |

_Response values:_

- If the operation has run _Successfully_, the JSON response is: [OK](#3321-ok).

- If the operation returns an _Error_, the JSON response is: [Error](#3322-error).

---

### 3.5.2. Adapter Commands Section

Perform operations on the Bluetooth adapter.

#### 3.5.2.1. adapter list

List all available Bluetooth adapters.

Options: None

_Response values:_
  
- If the operation has run _Successfully_, the JSON response is: [Adapters](#3326-adapters)

- If the operation returns an _Error_, the JSON response is: [Error](#3322-error).

#### 3.5.2.2. adapter properties

Get information about the Bluetooth adapter.

| Option    | Required | Default Value | Description                           |
| --------- | -------- | ------------- | ------------------------------------- |
| --address | False    | N/A           | The Bluetooth address of the adapter. |

_Response values:_

- If the operation has run _Successfully_, the JSON response is: [Adapter](#3326-adapter).

- If the operation returns an _Error_, the JSON response is: [Error](#3322-error).

#### 3.5.2.3. adapter discovery start

Start a device discovery.

| Option    | Required | Default Value | Description                                                                |
| --------- | -------- | ------------- | -------------------------------------------------------------------------- |
| --timeout | False    | 0             | A value in seconds which determines when the device discovery will finish. |
| --address | False    | N/A           | The Bluetooth address of the adapter.                                      |

_Response values:_

- If the operation has run _Successfully_, the JSON response is: [OK](#3321-ok).

- If the operation returns an _Error_, the JSON response is: [Error](#3322-error).

_Notes_:
A device discovery must run in the background.

The device discovery flow is as follows:
- The client calls this method, and waits for a status.
- The server starts the device discovery, and immediately returns an [OK](#3321-ok) response if the discovery has started,
or an [Error](#3322-error), if the discovery stopped with errors
- Once the discovery is in progress:
  - An [Adapter Event](#3326-adapter-event) must be sent with the **event_action** as **updated** and the **Discovering** property set to true.
  - Any discovered devices must be sent as a [Device Event](#3328-device-event) to the client, with:
    - The **event_action** as **added**, if devices are visible, or
    - The **event_action** as **removed**, if devices are no longer visible.
- When discovery has stopped:
  - An [Adapter Event](#3326-adapter-event) must be sent, with the **event_action** as **updated** and with the **Discovering** property set to false.

The **updated** event_action is not valid in this context.

#### 3.5.2.4. adapter discovery stop

Stop a device discovery (RPC only).

| Option    | Required | Default Value | Description                           |
| --------- | -------- | ------------- | ------------------------------------- |
| --address | False    | N/A           | The Bluetooth address of the adapter. |

_Response values:_

- If the operation has run _Successfully_, the JSON response is: [OK](#3321-ok).

- If the operation returns an _Error_, the JSON response is: [Error](#3322-error).

#### 3.5.2.5. adapter get-paired-devices

Get the list of paired devices.

| Option    | Required | Default Value | Description                           |
| --------- | -------- | ------------- | ------------------------------------- |
| --address | False    | N/A           | The Bluetooth address of the adapter. |

_Response values:_

- If the operation has run _Successfully_, the JSON response is: [Paired Devices](#3329-paired-devices)

- If the operation returns an _Error_, the JSON response is: [Error](#3322-error).

#### 3.5.2.6. adapter set-powered-state

Sets the power state of the adapter.

| Option    | Required | Default Value | Description                                  |
| --------- | -------- | ------------- | -------------------------------------------- |
| --state   | False    | On            | The adapter power state to set ("on"/"off"). |
| --address | False    | N/A           | The Bluetooth address of the adapter.        |

_Response values:_

- If the operation has run _Successfully_, the JSON response is: [OK](#3321-ok).

- If the operation returns an _Error_, the JSON response is: [Error](#3322-error).

#### 3.5.2.7. adapter set-pairable-state

Sets the pairable state of the adapter.

| Option    | Required | Default Value | Description                                     |
| --------- | -------- | ------------- | ----------------------------------------------- |
| --state   | False    | On            | The adapter pairable state to set ("on"/"off"). |
| --address | False    | N/A           | The Bluetooth address of the adapter.           |

_Response values:_

- If the operation has run _Successfully_, the JSON response is: [OK](#3321-ok).

- If the operation returns an _Error_, the JSON response is: [Error](#3322-error).

#### 3.5.2.8. adapter set-discoverable-state

Sets the discoverable state of the adapter.

| Option    | Required | Default Value | Description                                         |
| --------- | -------- | ------------- | --------------------------------------------------- |
| --state   | False    | On            | The adapter discoverable state to set ("on"/"off"). |
| --address | False    | N/A           | The Bluetooth address of the adapter.               |

_Response values:_

- If the operation has run _Successfully_, the JSON response is: [OK](#3321-ok).

- If the operation returns an _Error_, the JSON response is: [Error](#3322-error).

---

### 3.5.3. Device Commands Section

Perform operations on a Bluetooth device.

#### 3.5.3.1. device properties

Get information about a Bluetooth device.

| Option    | Required | Default Value | Description                          |
| --------- | -------- | ------------- | ------------------------------------ |
| --address | True     | N/A           | The Bluetooth address of the device. |

_Response values:_

- If the operation has run _Successfully_, the JSON response is: [Device](#3328-device).

- If the operation returns an _Error_, the JSON response is: [Error](#3322-error).

#### 3.5.3.2. device pair

Pair with a Bluetooth device.

| Option    | Required | Default Value | Description                                                                                              |
| --------- | -------- | ------------- | -------------------------------------------------------------------------------------------------------- |
| --timeout | False    | 10            | The maximum amount of time in seconds that a pairing request can wait for a reply during authentication. |
| --address | True     | N/A           | The Bluetooth address of the device.                                                                     |

_Response values:_

- If the operation has run _Successfully_, the JSON response is: [OK](#3321-ok).

- If the operation returns an _Error_, the JSON response is: [Error](#3322-error).

_Notes:_

When this method is called and a pairing operation is in progress, the remote or the host device may need to authenticate the paring process using a pin or a passkey. Therefore,
when appropriate, a [Pairing Authentication Event](#3335-pairing-authentication-event) must be sent to the client when authentication request is received from the host/remote device.

The authentication flow is as follows:

- The client first calls this method, and starts the pairing process.
- The pairing process is initiated, and the remote device generates a pin/passkey for authentication and asks the host (in this case the server) to verify.
- The server sends an appropriate [Pairing Authentication Event](#3335-pairing-authentication-event) to the client, with the **"auth_id"** and **"auth_reply_method"** properties set.
- The client receives the [Pairing Authentication Event](#3335-pairing-authentication-event), and calls the [rpc auth](#3514-rpc-auth) method with the `--response` and `--authentication-id` parameters set according to the provided **"auth_id"** and **"auth_reply_method"** properties from the event.
- Based on the client's response, the pairing is either completed if the client approves the request, or cancelled if the client does not approve the request.

For example, say the server sends a [Pairing Authentication Event](#3335-pairing-authentication-event) like so:
```
"pairing_auth_event": {
"auth_id": 2500,
"auth_event": "confirm-passkey",
"auth_reply_method": "reply-yes-no",
"timeout_ms": 10000,
"passkey": 123456
}
```

Then, the client receives the event, accepts the pairing, and would send a request to authenticate  the pairing process like so:
```
{
"request_id": 1200,
"command": ["rpc", "auth", "--authentication-id", "2500", "--response", "yes"]
}
```
Or, if the client does not want to pair with the remote device the request sent would be:
```
{
"request_id": 1200,
"command": ["rpc", "auth", "--authentication-id", "2500", "--response", ""]
}
```

#### 3.5.3.3. device pair cancel

Cancel an ongoing pairing session with a Bluetooth device.

| Option    | Required | Default Value | Description                          |
| --------- | -------- | ------------- | ------------------------------------ |
| --address | True     | N/A           | The Bluetooth address of the device. |

_Response values:_

- If the operation has run _Successfully_, the JSON response is: [OK](#3321-ok).

- If the operation returns an _Error_, the JSON response is: [Error](#3322-error).

#### 3.5.3.4. device connect

Connect to a Bluetooth device automatically.

| Option    | Required | Default Value | Description                          |
| --------- | -------- | ------------- | ------------------------------------ |
| --address | True     | N/A           | The Bluetooth address of the device. |

_Response values:_

- If the operation has run _Successfully_, the JSON response is: [OK](#3321-ok).

- If the operation returns an _Error_, the JSON response is: [Error](#3322-error).

#### 3.5.3.5. device connect profile

Connect to a device using a specified profile

| Option    | Required | Default Value | Description                              |
| --------- | -------- | ------------- | ---------------------------------------- |
| --uuid    | True     | N/A           | The Bluetooth service profile as a UUID. |
| --address | True     | N/A           | The Bluetooth address of the device.     |

_Response values:_

- If the operation has run _Successfully_, the JSON response is: [OK](#3321-ok).

- If the operation returns an _Error_, the JSON response is: [Error](#3322-error).

#### 3.5.3.6. device disconnect

Disconnect from a Bluetooth device.

| Option    | Required | Default Value | Description                          |
| --------- | -------- | ------------- | ------------------------------------ |
| --address | True     | N/A           | The Bluetooth address of the device. |

_Response values:_

- If the operation has run _Successfully_, the JSON response is: [OK](#3321-ok).

- If the operation returns an _Error_, the JSON response is: [Error](#3322-error).

#### 3.5.3.7. device remove

Remove a Bluetooth device.

| Option    | Required | Default Value | Description                          |
| --------- | -------- | ------------- | ------------------------------------ |
| --address | True     | N/A           | The Bluetooth address of the device. |

_Response values:_

- If the operation has run _Successfully_, the JSON response is: [OK](#3321-ok).

- If the operation returns an _Error_, the JSON response is: [Error](#3322-error).

#### 3.5.3.8. device opp start-session

Start an Object Push client session with a device.

| Option    | Required | Default Value | Description                          |
| --------- | -------- | ------------- | ------------------------------------ |
| --address | True     | N/A           | The Bluetooth address of the device. |

_Response values:_

- If the operation has run _Successfully_, the JSON response is: [OK](#3321-ok).

- If the operation returns an _Error_, the JSON response is: [Error](#3322-error).

_Notes:_
A session must run in the background. 

Once a session is started with a device, this method call must return an [OK](#3321-ok) response immediately, or an [Error](#3322-error) if the session could not be established. 

A session should be able to queue files that are sent by the client, and send each
file one-by-one to the remote device. 

For each outgoing file transfer, [File Transfer Events](#33210-file-transfer-events) must be sent to all connected clients. 

Each file transfer event must have an **"updated"** [event action](#333-events).
Ensure that the **"status"** property for each [File Transfer Event](#33210-file-transfer-events) is set to:
-  `"active"` for the ongoing file transfer,
- `"complete"` for the completed file transfer, and
- `"error"` if any errors occurred during the file transfer.
- `"suspended"` if the client has requested for the transfer to be paused.

The `queued` **"status"** value is not valid in this context.

#### 3.5.3.9. device opp send-file

Send a file to a device with an open session. Use "[device opp start-session](#3328-device-opp-start-session)" to start a session with a device first before calling this method.

| Option | Required | Default Value | Description                      |
| ------ | -------- | ------------- | -------------------------------- |
| --file | True     | N/A           | A full path of the file to send. |

_Response values:_

- If the operation has run _Successfully_, the JSON response is: [File Transfer](#33210-file-transfer)

- If the operation returns an _Error_, the JSON response is: [Error](#3322-error).

_Notes:_

This method effectively only queues the file to be sent within the current session,
so it should immediately return:

- A [File Transfer](#33210-file-transfer) response with the **"status"** property set only to `"queued"` if the provided file was queued for transfer, or
- An [Error](#3322-error) if the provided file was not queued for transfer.

#### 3.5.3.10. device opp cancel-transfer

Cancel an existing Object Push file transfer session with a device. This can be any
currently running transfer, for example if the Object Push server is receiving a file,
or a file is currently being sent to the device after setting up an Object Push session.

| Option    | Required | Default Value | Description                          |
| --------- | -------- | ------------- | ------------------------------------ |
| --address | True     | N/A           | The Bluetooth address of the device. |

_Response values:_

- If the operation has run _Successfully_, the JSON response is: [OK](#3321-ok).

- If the operation returns an _Error_, the JSON response is: [Error](#3322-error).

#### 3.5.3.11. device opp suspend-transfer

Suspend an existing Object Push file transfer session with a device.

Options: None

_Response values:_

- If the operation has run _Successfully_, the JSON response is: [OK](#3321-ok).

- If the operation returns an _Error_, the JSON response is: [Error](#3322-error).

#### 3.5.3.12. device opp resume-transfer

Resume an existing Object Push file transfer session with a device.

Options: None

_Response values:_

- If the operation has run _Successfully_, the JSON response is: [OK](#3321-ok).

- If the operation returns an _Error_, the JSON response is: [Error](#3322-error).

#### 3.5.3.13. device opp stop-session

Stop an existing Object Push client session with a device.

Options: None

_Response values:_

- If the operation has run _Successfully_, the JSON response is: [OK](#3321-ok).

- If the operation returns an _Error_, the JSON response is: [Error](#3322-error).

#### 3.5.3.14. device opp start-server

Start an Object Push server. The Object Push server will listen for any incoming files
and send file-transfer events accordingly.

| Option      | Required | Default Value | Description                                                 |
| ----------- | -------- | ------------- | ----------------------------------------------------------- |
| --directory | False    | N/A           | A full path to a directory to save incoming file transfers. |

_Response values:_

- If the operation has run _Successfully_, the JSON response is: [OK](#3321-ok).

- If the operation returns an _Error_, the JSON response is: [Error](#3322-error).

_Notes_:

An Object push server must be started in the background, and once a server is started, this method call must return an [OK](#3321-ok) response immediately, or an [Error](#3322-error) if the server could not be started. 

For each incoming file transfer, the default directory to receive files should be within
the following well defined paths:
	
| OS                                | Full path to received file cache folder                                                                         |
| --------------------------------- | --------------------------------------------------------------------------------------------------------------- |
| Windows                           | `%LocalAppData%\haraltd\transfers`                                                                              |
| MacOS                             | `$HOME/Library/Caches/haraltd/transfers`                                                                        |
| Other UNIX like operating systems | If **$XDG_CACHE_HOME** is defined, ` $XDG_CACHE_HOME/haraltd/transfers`, else ` $HOME/.cache/haraltd/transfers` |

For any incoming file transfers, [File Transfer Events](#33210-file-transfer-events) must be sent to all connected clients. 

Each file transfer event must have an **"updated"** [event action](#333-events).
Ensure that the **"status"** property for each [File Transfer Event](#33210-file-transfer-events) is set to:
-  `"active"` for the ongoing file transfer,
- `"complete"` for the completed file transfer, and
- `"error"` if any errors occurred during the file transfer.

The `queued` and `suspended` **"status"** values are not valid in this context.

#### 3.5.3.15. device opp stop-server

Stop a started Object Push server.

| Option      | Required | Default Value | Description                                                 |
| ----------- | -------- | ------------- | ----------------------------------------------------------- |
| --directory | False    | N/A           | A full path to a directory to save incoming file transfers. |

_Response values:_

- If the operation has run _Successfully_, the JSON response is: [OK](#3321-ok).

- If the operation returns an _Error_, the JSON response is: [Error](#3322-error).

---
