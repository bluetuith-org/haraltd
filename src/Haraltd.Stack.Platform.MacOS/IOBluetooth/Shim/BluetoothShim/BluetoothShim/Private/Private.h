//
//  Private.h
//  BluetoothShim
//
//  Created by Kendrick on 08/10/25.
//

#import <IOBluetooth/IOBluetooth.h>
#import "IOBluetoothDevice+Extended.h"
#import "IOBluetoothCoreBluetoothCoordinator.h"
#import "CBClassicManager.h"
#import "CBClassicManager+RemovePeer.h"
#import "CBClassicPeer.h"
#import "PrivateMethodAttachments.h"

CBClassicPeer* getPeerFromDictionary(NSDictionary* dict, CBClassicManager* manager);
IOBluetoothDevice* getDeviceFromDictionary(NSDictionary* dict, CBClassicManager* manager);
IOBluetoothDevice* classicPeerToDevice(CBClassicPeer* peer);
NSArray<CBClassicPeer *>* getPairedPeers(void);
NSArray<IOBluetoothDevice *>* getPairedDevices(void);
void purgeDeviceFromCache(IOBluetoothDevice* device);
