//
//  IOBluetoothDevice+Extended.h
//  BluetoothShim
//
//  Created by Kendrick on 29/10/25.
//

#import <IOBluetooth/IOBluetooth.h>
#import "CBClassicPeer.h"

@interface IOBluetoothDevice (Extended)
+ (void) removeUniqueObject:(id)object;
+ (IOBluetoothDevice*) deviceWithCBClassicPeer:(CBClassicPeer*)peer;
- (void) setAddress:(const BluetoothDeviceAddress *)address;
- (CBClassicPeer*) classicPeer;
- (void) remove;
- (void) setPeripheral:(id)peripheral;
- (void) setPeer:(CBClassicPeer*)peer;
@end
