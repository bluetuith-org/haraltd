//
//  CBClassicManager.h
//  BluetoothShim
//
//  Created by Kendrick on 29/10/25.
//

#import <IOBluetooth/IOBluetooth.h>
#import "CBClassicPeer.h"

@interface CBClassicManager : NSObject
- (CBClassicPeer *) classicPeerWithInfo:(NSDictionary *)info;
- (id) sendSyncMsg:(unsigned short)msgId args:(NSDictionary *)argDict;
- (NSMapTable*) peers;
@end
