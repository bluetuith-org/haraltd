using System;
using System.Runtime.InteropServices;
using CoreFoundation;
using Foundation;
using IOBluetooth;
using ObjCRuntime;

namespace IOBluetooth.Shim;

// audit-objc-generics: @interface Result<T> : NSObject
[BaseType(typeof(NSObject))]
interface ResultWithError<T>
{
    // @property (nonatomic, strong) NSError * _Nullable error;
    [NullAllowed, Export("error", ArgumentSemantic.Strong)]
    NSError Error { get; set; }

    // @property (nonatomic, strong) T _Nullable result;
    [Export("result", ArgumentSemantic.Strong)]
    T Value { get; set; }
}

// @interface BluetoothShim : NSObject
[BaseType(typeof(NSObject))]
interface BluetoothShim
{
    [Static]
    [Export("tryInitializeCoordinator")]
    NSError TryInitCoordinator();

    // +(NSString *)getHostControllerAddressString;
    [Static]
    [Export("getHostControllerAddressString")]
    string GetHostControllerAddressString();

    // +(Result<NSArray<IOBluetoothDevice *> *> *)getPairedDevices;
    [Static]
    [Export("getPairedDevices")]
    ResultWithError<NSArray<BluetoothDevice>> GetPairedDevices();

    // +(Result<NSNumber *> *)getBluetoothPermissionStatus;
    [Static]
    [Export("getBluetoothPermissionStatus")]
    ResultWithError<NSNumber> GetBluetoothPermissionStatus();

    // +(IOBluetoothDevice *)getKnownDevice:(NSString *)address isKnown:(_Bool *)known;
    [Static]
    [Export("getKnownDevice:isKnown:")]
    BluetoothDevice GetKnownDevice(string address, ref bool known);

    // +(void)removeKnownDevice:(IOBluetoothDevice*)device;
    [Static]
    [Export("removeKnownDevice:")]
    void RemoveKnownDevice(BluetoothDevice device);

    //+(IOBluetoothDevice*)setNilPeripheral:(IOBluetoothDevice*)device
    [Static]
    [Export("setNilPeripheral:")]
    BluetoothDevice SetNilPeripheral(BluetoothDevice device);

    // +(_Bool)connectDevice:(NSString *)address;
    [Static]
    [Export("connectDevice:")]
    bool ConnectDevice(string address);
}

// @protocol BluetoothEventDelegate <NSObject>
[Protocol, Model]
[BaseType(typeof(NSObject))]
interface BluetoothEventDelegate
{
    // @required -(void)handleAdapterEvent:(struct AdapterEventData)data;
    [Abstract]
    [Export("handleAdapterEvent:")]
    void HandleAdapterEvent(AdapterEventData data);

    // @required -(void)handleDeviceEvent:(IOBluetoothDevice *)device eventAction:(EventAction)action additionalData:(struct DeviceEventData)data;
    [Abstract]
    [Export("handleDeviceEvent:eventAction:additionalData:")]
    void HandleDeviceEvent(BluetoothDevice device, EventAction action, DeviceEventData data);
}

// @interface BluetoothEvents : NSObject
[BaseType(typeof(NSObject))]
interface BluetoothEvents
{
    // +(id)tryInitWithDelegate:(id<BluetoothEventDelegate>)delegate;
    [Static]
    [Export("tryInitWithDelegate:")]
    ResultWithError<BluetoothEvents> TryInitWithDelegate(BluetoothEventDelegate @delegate);
}
