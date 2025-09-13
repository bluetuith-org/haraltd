//
//  CBClassicManager+RemovePeer.h
//  BluetoothShim
//
//  Created by Kendrick on 06/11/25.
//

#import "CBClassicManager.h"
#import "CBClassicPeer.h"

@interface CBClassicManager (RemovePeer)
- (void) removePeerFromStore:(CBClassicPeer*)peer;
@end
