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
                        .GetValueAsElementList();

                    // values in a list from most to least specific so read the first entry
                    var mostSpecific = vals.FirstOrDefault();
                    // short ids are automatically converted to a long Guid
                    protocolUuid = mostSpecific.GetValueAsUuid();
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
                    var guid = mostSpecific.GetValueAsUuid();

                    services.Add(guid);
                }
                catch { }
        }

        return [.. services];
    }

    internal static bool ParseAepId(
        string aepId,
        out string adapterAddress,
        out string deviceAddress
    )
    {
        adapterAddress = "";
        deviceAddress = "";

        var removeIdentifier = "Bluetooth#Bluetooth";
        if (aepId.StartsWith("BluetoothLE"))
            removeIdentifier = "BluetoothLE#BluetoothLE";

        aepId = aepId.Remove(0, removeIdentifier.Length);

        var addresses = aepId.Split("-");
        if (addresses.Length == 0)
            return false;

        adapterAddress = addresses[0];
        deviceAddress = addresses[1];

        return true;
    }

    internal static string HostAddress(string address)
    {
        return string.IsNullOrEmpty(address) ? "" : address.Replace("(", "").Replace(")", "");
    }

#nullable enable
    internal static async Task<BluetoothDevice> GetBluetoothDeviceWithService(
        string address,
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

    internal static async Task<BluetoothDevice> GetBluetoothDevice(string address)
    {
        AdapterMethods.ThrowIfRadioNotOperable();

        var device = await BluetoothDevice.FromBluetoothAddressAsync(
            BluetoothAddress.Parse(address)
        );
        return device
            ?? throw new ArgumentNullException($"The device with address {address} was not found.");
    }

    internal static async Task<string> GetDeviceIdBySelector(string address, string selector)
    {
        var deviceId = "";
        ulong addressAsUlong = BluetoothAddress.Parse(address);

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
