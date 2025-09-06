using System.Runtime.InteropServices.WindowsRuntime;
using Haraltd.Stack.Microsoft.Adapters;
using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Bluetooth.AttributeIds;
using InTheHand.Net.Bluetooth.Sdp;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace Haraltd.Stack.Microsoft.Devices;

internal static class DeviceUtils
{
    internal static Guid[] GetServicesFromSdpRecords(IReadOnlyList<IBuffer> sdpRecords)
    {
        var services = new List<Guid>();

        foreach (var sdpRecord in sdpRecords)
        {
            var protocolUuid = Guid.Empty;

            var attributes = ServiceRecord.CreateServiceRecordFromBytes(sdpRecord.ToArray());

            var attribute = attributes.GetAttributeById(
                UniversalAttributeId.ProtocolDescriptorList
            );
            if (attribute != null)
                try
                {
                    var vals = attribute
                        .Value.GetValueAsElementList()
                        .FirstOrDefault()
                        ?.GetValueAsElementList();

                    // values in a list from most to least specific so read the first entry
                    if (vals != null)
                    {
                        var mostSpecific = vals.FirstOrDefault();
                        // short ids are automatically converted to a long Guid
                        if (mostSpecific != null)
                            protocolUuid = mostSpecific.GetValueAsUuid();
                    }
                }
                catch
                {
                    protocolUuid = Guid.Empty;
                }

            if (protocolUuid != BluetoothProtocol.L2CapProtocol)
                continue;

            attribute = attributes.GetAttributeById(UniversalAttributeId.ServiceClassIdList);
            if (attribute != null)
                try
                {
                    var vals = attribute.Value.GetValueAsElementList();
                    // values in a list from most to least specific so read the first entry
                    var mostSpecific = vals.FirstOrDefault();
                    // short ids are automatically converted to a long Guid
                    if (mostSpecific != null)
                    {
                        var guid = mostSpecific.GetValueAsUuid();

                        services.Add(guid);
                    }
                }
                catch
                {
                    // ignored
                }
        }

        return [.. services];
    }

    internal static bool ParseAepIdAsString(
        string aepId,
        out string adapterAddress,
        out string deviceAddress
    )
    {
        adapterAddress = null;
        deviceAddress = null;

        var removeIdentifier = "Bluetooth#Bluetooth";
        if (aepId.StartsWith("BluetoothLE"))
            removeIdentifier = "BluetoothLE#BluetoothLE";

        aepId = aepId.Remove(0, removeIdentifier.Length);

        var addresses = aepId.Split("-");
        if (addresses.Length == 0)
            return false;

        try
        {
            adapterAddress = addresses[0];
            deviceAddress = addresses[1];
        }
        catch
        {
            return false;
        }

        return true;
    }

    internal static bool ParseAepId(
        string aepId,
        out BluetoothAddress adapterAddress,
        out BluetoothAddress deviceAddress
    )
    {
        adapterAddress = null;
        deviceAddress = null;

        if (ParseAepIdAsString(aepId, out var adapterAddr, out var deviceAddr))
        {
            adapterAddress = BluetoothAddress.Parse(adapterAddr);
            deviceAddress = BluetoothAddress.Parse(deviceAddr);

            return true;
        }

        return false;
    }

#nullable enable
    internal static async Task<BluetoothDevice> GetBluetoothDeviceWithService(
        BluetoothAddress address,
        Guid service
    )
    {
        var device = await GetBluetoothDevice(address);
        if (!device.DeviceInformation.Pairing.IsPaired)
            throw new ArgumentException($"Device {address} is not paired");

        var result = await device.GetRfcommServicesForIdAsync(RfcommServiceId.FromUuid(service));
        return result is null || result.Services.Count == 0
            ? throw new ArgumentException(
                $"The service ({BluetoothService.GetName(service)}) does not exist for device with address {address}"
            )
            : device;
    }

    internal static async Task<BluetoothDevice> GetBluetoothDevice(BluetoothAddress address)
    {
        WindowsAdapter.ThrowIfRadioNotOperable();

        var device = await BluetoothDevice.FromBluetoothAddressAsync(address);
        return device
            ?? throw new ArgumentNullException($"The device with address {address} was not found.");
    }

    internal static async Task<string> GetDeviceIdBySelector(
        BluetoothAddress address,
        string selector
    )
    {
        var deviceId = "";
        ulong addressAsUlong = address;

        foreach (var deviceInfo in await DeviceInformation.FindAllAsync(selector))
        {
            var bluetoothDevice = await BluetoothDevice.FromIdAsync(deviceInfo.Id);
            if (bluetoothDevice?.BluetoothAddress == addressAsUlong)
            {
                deviceId = deviceInfo.Id;
                break;
            }
        }

        return string.IsNullOrEmpty(deviceId)
            ? throw new ArgumentException(
                $"The device with address {address} was not found with selector {selector}"
            )
            : deviceId;
    }
}
