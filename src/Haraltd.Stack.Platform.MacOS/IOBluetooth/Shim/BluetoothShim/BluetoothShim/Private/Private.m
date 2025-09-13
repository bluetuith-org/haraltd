//
//  Private.m
//  BluetoothShim
//
//  Created by Kendrick on 08/10/25.
//

#import <Foundation/Foundation.h>
#import <IOBluetooth/IOBluetooth.h>
#import "Private.h"
#import "PrivateMethodAttachments.h"

CBClassicPeer* getPeerFromDictionary(NSDictionary* dict, CBClassicManager* manager) {
    CBClassicManager* classicManager = manager;
    if (classicManager == nil)
        classicManager = [[IOBluetoothCoreBluetoothCoordinator sharedInstance] classicManager];
    
    CBClassicPeer* peer = [classicManager classicPeerWithInfo:dict];
    if (peer == nil || [[peer addressString] length] == 0)
        return nil;
    
    return peer;
}

IOBluetoothDevice* getDeviceFromDictionary(NSDictionary* dict, CBClassicManager* manager) {
    CBClassicPeer* peer = getPeerFromDictionary(dict, manager);
    
    return classicPeerToDevice(peer);
}

IOBluetoothDevice* classicPeerToDevice(CBClassicPeer* peer) {
    if (peer == nil)
        return nil;
    
    IOBluetoothDevice* device = [IOBluetoothDevice deviceWithAddressString:[peer addressString]];
    if ([device classicPeer] == nil || [[[device classicPeer] addressString] length] == 0)
        device = nil;
    
    return device;
}

bool enumeratePairedPeers(void (^handlePeer)(CBClassicPeer* peer)) {
    NSArray* peers = [[IOBluetoothCoreBluetoothCoordinator sharedInstance] pairedDevices];
    if (peers == nil || [peers count] == 0)
        return false;
    
    [peers enumerateObjectsUsingBlock:^(id  _Nonnull peer, NSUInteger idx, BOOL * _Nonnull stop) {
        if ([peer isKindOfClass:[CBClassicPeer class]]) {
            handlePeer(peer);
        }
    }];
    
    return true;
}

NSArray<CBClassicPeer *>* getPairedPeers(void) {
    __block NSMutableArray* peers = [[NSMutableArray alloc] init];
    
    enumeratePairedPeers(^(CBClassicPeer *peer) {
        [peers addObject:peer];
    });
    
    return [peers copy];
}

NSArray<IOBluetoothDevice *>* getPairedDevices(void) {
    __block NSMutableArray* devices = [[NSMutableArray alloc] init];
    
    enumeratePairedPeers(^(CBClassicPeer *peer) {
        IOBluetoothDevice* device = classicPeerToDevice(peer);
        if (!device)
            return;
        
        [devices addObject:device];
    });
    

    return [devices copy];
}

// TODO: Purge this function. Calling this should not be required.
void purgeDeviceFromCache(IOBluetoothDevice* device) {
    @try {
        [IOBluetoothDevice removeUniqueObject:device];
        
        [[[IOBluetoothCoreBluetoothCoordinator sharedInstance] classicManager] removePeerFromStore:[device classicPeer]];
    } @catch (NSException *exception) {
        NSLog(@"purgeDeviceFromCache exception: %@", exception);
    }
}
