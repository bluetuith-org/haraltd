//
//  IOBluetoothCoreBluetoothCoordinator.h
//  BluetoothShim
//
//  Created by Kendrick on 29/10/25.
//

#import <IOBluetooth/IOBluetooth.h>
#import "CBClassicManager.h"

@protocol IOBluetoothCoreBluetoothCoordinatorDelegate <NSObject>
@optional
- (void) peerPairingCompleted:(CBClassicPeer*)peer withError:(long)error;
- (void) inquiryStateChanged:(int)inquiryState;
- (void) peerConnected:(CBClassicPeer*)peer error:(long)error;
- (void) peerDisconnected:(CBClassicPeer*)peer withError:(long)error;
- (void) peerDiscovered:(CBClassicPeer*)peer withResults:(id)results;
- (void) peerUnpaired:(CBClassicPeer*)peer;
- (void) peerUpdated:(CBClassicPeer*)peer withResults:(id)results;
@end

@interface IOBluetoothCoreBluetoothCoordinator
- (instancetype) sharedInstance;
- (CBClassicManager *) classicManager;
- (NSArray<IOBluetoothDevice *> *) pairedDevices;
- (long) getTCCApprovalStatus;
- (long) inquiryState;
- (void) addDelegate:(id<IOBluetoothCoreBluetoothCoordinatorDelegate>)delegate;
- (void) removeDelegate:(id<IOBluetoothCoreBluetoothCoordinatorDelegate>)delegate;
@end
