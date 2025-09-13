//
//  CBClassicManager+RemovePeer.m
//  BluetoothShim
//
//  Created by Kendrick on 06/11/25.
//

#import "CBClassicManager+RemovePeer.h"
#import <objc/objc-sync.h>

@implementation CBClassicManager (RemovePeer)
- (void) removePeerFromStore:(CBClassicPeer *)peer {
    if (peer == nil)
        return;
    
    [peer setOrphan];
    
    NSMapTable* peers = [self peers];

    objc_sync_enter(peers);
    [peers removeObjectForKey: [peer identifier]];
    objc_sync_exit(peers);
}
@end
