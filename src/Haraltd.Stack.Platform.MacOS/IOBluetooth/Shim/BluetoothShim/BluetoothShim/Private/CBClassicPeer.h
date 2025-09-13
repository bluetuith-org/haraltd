//
//  CBClassicPeer.h
//  BluetoothShim
//
//  Created by Kendrick on 29/10/25.
//

#import <IOBluetooth/IOBluetooth.h>

@interface CBClassicPeer : NSObject
- (instancetype) initWithInfo:(id)infoDict manager:(id)classicManager;
- (NSUUID *) identifier;
- (NSString *) addressString;
- (void) setBatteryPercentSingle:(int)batteryPercent;
- (void) setBatteryPercentCombined:(int)batteryPercent;
- (void) setOrphan;
@end
