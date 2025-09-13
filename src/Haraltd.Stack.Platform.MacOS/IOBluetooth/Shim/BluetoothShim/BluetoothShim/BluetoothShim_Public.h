//
//  Base.h
//  BluetoothShim
//
//  Created by Kendrick on 22/09/25.
//

#import <IOBluetooth/IOBluetooth.h>
#import <BluetoothShim/Result.h>

@interface BluetoothShim : NSObject
+ (NSError*) tryInitializeCoordinator;
+ (NSString*) getHostControllerAddressString;
+ (ResultWithError<NSArray<IOBluetoothDevice *> *>*) getPairedDevices;
+ (ResultWithError<NSNumber *> *) getBluetoothPermissionStatus;
+ (IOBluetoothDevice*) getKnownDevice:(NSString*)address isKnown:(bool*)known;
+ (IOBluetoothDevice*) setNilPeripheral:(IOBluetoothDevice*)device;
+ (void) removeKnownDevice:(IOBluetoothDevice*)device;
@end
