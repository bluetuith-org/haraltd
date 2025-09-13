//
//  DeviceEventHandler.m
//  BluetoothShim
//
//  Created by Kendrick on 12/10/25.
//

#import <Foundation/Foundation.h>
#import <BluetoothShim/BluetoothEvents.h>
#import <notify.h>
#import <pthread/pthread.h>
#include <IOKit/ps/IOPowerSources.h>
#import "BluetoothEventsCoordinator.h"
#import "DeviceEventHandler.h"

PSPropertyName const PSPropTransportType = @"Transport Type";
PSPropertyName const PSPropTransportName = @"Bluetooth";
PSPropertyName const PSPropAccessoryIdentifier = @"Accessory Identifier";
PSPropertyName const PSPropCurrentBatteryPercent = @"Current Capacity";

@implementation DeviceEventHandler {
    __weak BluetoothEventsCoordinator* eh;
    
    NSMapTable<NSUUID*, CBClassicPeer*>* _classicPeers;
    dispatch_queue_t queue;
    NSArray<IOBluetoothDevice*>* _cachedDevices;
    
    int accAttachNotificationToken;
    int accPowerNotificationToken;
    NSMutableDictionary<NSString*, NSDictionary*>* _batteryInformation;
    
    NSMutableArray<dispatch_block_t>* scheduledBlocks;
    pthread_mutex_t schedMutex;
}

- (void) dealloc {
    dispatch_suspend(queue);
    [self modifyScheduledBlock:nil mode:ScheduledBlocksClearAll];
    dispatch_resume(queue);
    
    dispatch_sync(queue, ^{
        [[IOBluetoothCoreBluetoothCoordinator sharedInstance] removeDelegate:self];
        notify_cancel(accAttachNotificationToken);
        notify_cancel(accPowerNotificationToken);
    });
    
    pthread_mutex_destroy(&schedMutex);
}

- (instancetype) init {
    self = [super init];
    if (!self)
        return nil;

    dispatch_queue_attr_t queue_attr = dispatch_queue_attr_make_with_autorelease_frequency(0, DISPATCH_AUTORELEASE_FREQUENCY_WORK_ITEM);
    queue = dispatch_queue_create("com.haraltd.events.DeviceQueue", queue_attr);
    
    pthread_mutex_init(&schedMutex, NULL);
    
    dispatch_suspend(queue);
    
    return self;
}

- (void) setHandler:(BluetoothEventsCoordinator*)handler {
    eh = handler;
}

- (NSError *) start {
    NSError *error;

    error = [self updatePairedDevices];
    if (error != nil)
        return error;
    
    [self registerForBatteryNotifications];
    [[IOBluetoothCoreBluetoothCoordinator sharedInstance] addDelegate:self];
    
    _cachedDevices = @[
        [[IOBluetoothDevice alloc] init],
        [[IOBluetoothDevice alloc] init],
        [[IOBluetoothDevice alloc] init]
    ];
    
    dispatch_resume(queue);
    
    return error;
}

- (void) registerForBatteryNotifications {
    __block __weak DeviceEventHandler* weakSelf = self;
    
    int status = notify_register_dispatch("com.apple.system.accpowersources.attach", &accAttachNotificationToken, queue, ^(int token) {
        [weakSelf updateBatterySources];
    });
    if (status != NOTIFY_STATUS_OK) {
        NSLog(@"Could not register for accessory attach power notification");
        return;
    }

    status = notify_register_dispatch("com.apple.system.accpowersources.timeremaining", &accPowerNotificationToken, queue, ^(int token) {
        [weakSelf updateBatterySourceInfo];
    });
    if (status != NOTIFY_STATUS_OK) {
        NSLog(@"Could not register for accessory source notification");
        return;
    }
    
    _batteryInformation = [[NSMutableDictionary alloc] init];
    [self updateBatterySources];
}

- (NSError*) updatePairedDevices {
    NSArray* peers;
    @try {
        peers = getPairedPeers();
    } @catch (NSException *exception) {
        return [NSError errorWithDomain:[exception name] code:1 userInfo:[exception userInfo]];
    }

    _classicPeers = [[NSMapTable alloc] init];
    if (peers != nil) {
        [peers enumerateObjectsUsingBlock:^(CBClassicPeer* _Nonnull peer, NSUInteger idx, BOOL * _Nonnull stop) {
            [self addPairedPeer:peer];
        }];
    }

    return nil;
}

- (CBClassicPeer*) getPairedPeer:(NSUUID*)identifier {
    return [_classicPeers objectForKey:identifier];
}

- (CBClassicPeer*) getPairedPeerByAddress:(NSString*)address {
    if (address == nil)
        return nil;
    
    for (NSUUID* pairedPeerIdentifier in _classicPeers) {
        if (pairedPeerIdentifier == nil)
            continue;
        
        CBClassicPeer* peer = [_classicPeers objectForKey:pairedPeerIdentifier];
        if (peer == nil)
            continue;
        
        NSString* pairedAddress = [peer addressString];
        
        if (pairedAddress != nil && [pairedAddress compare:address options:NSCaseInsensitiveSearch] == NSOrderedSame) {
            return peer;
        }
    }
    
    return nil;
}

- (void) addPairedPeer:(CBClassicPeer*)peer {
    [_classicPeers setObject:peer forKey:[peer identifier]];
}

- (void) removePairedPeer:(NSUUID*)identifier {
    [_classicPeers removeObjectForKey:identifier];
}

- (void) addDevice:(CBClassicPeer*)peer {
    NSUUID* peerIdentifier = [peer identifier];
    if (peerIdentifier == nil) {
        return;
    }
    
    [self dispatchBlockForCachedDevice:CachedDeviceToAdd block:^(DeviceEventHandler *eventInstanceSelf, IOBluetoothDevice *device) {
        if ([eventInstanceSelf getPairedPeer:peerIdentifier] != nil) {
            return;
        }
        
        [eventInstanceSelf addPairedPeer:peer];
        
        struct DeviceEventData data = {};
        [eventInstanceSelf sendDeviceEvent:device forPeer:[eventInstanceSelf getPairedPeer:peerIdentifier] eventAction:kEventActionAdded additionalData:data];
    }];
}

- (void) removeDevice:(CBClassicPeer*)peer {
    NSUUID* peerIdentifier = [peer identifier];
    if (peerIdentifier == nil)
        return;

    [self dispatchBlockForCachedDevice:CachedDeviceToRemove block:^(DeviceEventHandler *eventInstanceSelf, IOBluetoothDevice *device) {
        struct DeviceEventData data = {};
        [eventInstanceSelf sendDeviceEvent:device forPeer:peer eventAction:kEventActionRemoved additionalData:data];
        
        [eventInstanceSelf removePairedPeer:peerIdentifier];
    }];
}

- (void) updateDevice:(CBClassicPeer*)peer {
    NSUUID* peerIdentifier = [peer identifier];
    if (peerIdentifier == nil) {
        return;
    }
    
    [self dispatchBlockForCachedDevice:CachedDeviceToUpdate block:^(DeviceEventHandler *eventInstanceSelf, IOBluetoothDevice *device) {
        struct DeviceEventData data = {};
        [eventInstanceSelf sendDeviceEvent:device forPeer:[eventInstanceSelf getPairedPeer:peerIdentifier] eventAction:kEventActionUpdated additionalData:data];
    }];
}

- (void) updateBatterySources {
    [self dispatchBlockForCachedDevice:CachedDeviceToUpdate block:^(DeviceEventHandler *eventInstanceSelf, IOBluetoothDevice *device) {
        [eventInstanceSelf enumerateBluetoothPowerSources:false applyFiltered:^(NSDictionary *dict, bool added) {
            NSString* addrString = dict[PSPropAccessoryIdentifier];
            int currentBatteryPercent = [dict[PSPropCurrentBatteryPercent] intValue];
                
            struct DeviceEventData data = {.BatteryInfo = {.modified = true, .BatteryPercentSingle = currentBatteryPercent, .BatteryPercentCombined = currentBatteryPercent}};
            [eventInstanceSelf sendDeviceEvent:device forPeer:[eventInstanceSelf getPairedPeerByAddress:addrString] eventAction:kEventActionUpdated additionalData:data];
        }];
    }];
}

- (void) updateBatterySourceInfo {
    [self dispatchBlockForCachedDevice:CachedDeviceToUpdate block:^(DeviceEventHandler *eventInstanceSelf, IOBluetoothDevice *device) {
        [eventInstanceSelf enumerateBluetoothPowerSources:true applyFiltered:^(NSDictionary *dict, bool added) {
            NSString* address = dict[PSPropAccessoryIdentifier];
            int currentCapacity = [dict[PSPropCurrentBatteryPercent] intValue];
            
            struct DeviceEventData data = {.BatteryInfo = {.modified = true, .BatteryPercentSingle = currentCapacity, .BatteryPercentCombined = currentCapacity}};
            [eventInstanceSelf sendDeviceEvent:device forPeer:[eventInstanceSelf getPairedPeerByAddress:address] eventAction:kEventActionUpdated additionalData:data];
        }];
    }];
}

- (void) sendDeviceEvent:(IOBluetoothDevice*)device forPeer:(CBClassicPeer*)peer eventAction:(EventAction)action additionalData:(struct DeviceEventData)data {
    if (peer == nil) {
        return;
    }
    
    IOReturn ret = [self appendPeerToDevice:peer forDevice:device];
    if (ret != kIOReturnSuccess)
        return;
    
    for (id<BluetoothEventDelegate> delegate in [eh getDelegates]) {
        [delegate handleDeviceEvent:device eventAction:action additionalData:data];
    }
    
    [device setPeer:nil];
}

- (void) enumerateBluetoothPowerSources:(bool)onlyUpdatedSources applyFiltered:(void (^)(NSDictionary* dict, bool added))applyBlock {
    CFTypeRef powerSourcesInfo = IOPSCopyPowerSourcesByType(4);
    if (powerSourcesInfo == nil)
        return;
    
    CFArrayRef powerSources = IOPSCopyPowerSourcesList(powerSourcesInfo);
    CFRelease(powerSourcesInfo);
    
    NSArray<NSDictionary*>* psArray = (__bridge_transfer NSArray*)powerSources;
    if (psArray == nil || [psArray count] == 0) {
        [_batteryInformation removeAllObjects];
        return;
    }
    
    NSMutableArray<NSString*>* powerSourcesToRemove;
    if (!onlyUpdatedSources && [_batteryInformation count] > 0) {
        powerSourcesToRemove = [[NSMutableArray alloc] init];
        [_batteryInformation enumerateKeysAndObjectsUsingBlock:^(NSString * _Nonnull key, NSDictionary * _Nonnull powerSourceInfo, BOOL * _Nonnull stop) {
            [powerSourcesToRemove addObject:[powerSourceInfo valueForKey:PSPropAccessoryIdentifier]];
        }];
    }
    
    [psArray enumerateObjectsUsingBlock:^(NSDictionary * _Nonnull powerSourceInfo, NSUInteger idx, BOOL * _Nonnull stop) {
        NSString* transportType = [powerSourceInfo valueForKey:PSPropTransportType];
        NSString* deviceAddress = [powerSourceInfo valueForKey:PSPropAccessoryIdentifier];
        id currentBatteryPercent = [powerSourceInfo valueForKey:PSPropCurrentBatteryPercent];
        
        if (transportType == nil || deviceAddress == nil || currentBatteryPercent == nil)
            return;
        
        if (![transportType isEqualToString:PSPropTransportName])
            return;
        
        NSDictionary* cachedDict = _batteryInformation[deviceAddress];
        
        _batteryInformation[deviceAddress] = powerSourceInfo;
        [powerSourcesToRemove removeObject:deviceAddress];
        
        if (cachedDict == nil && !onlyUpdatedSources) {
            applyBlock(powerSourceInfo, true);
            return;
        }
        
        if (onlyUpdatedSources) {
            int prevCapacity = [cachedDict[PSPropCurrentBatteryPercent] intValue];
            int currentCapacity = [currentBatteryPercent intValue];
            
            if (prevCapacity != currentCapacity)
                applyBlock(powerSourceInfo, false);
        }
    }];
    
    if (powerSourcesToRemove != nil && [powerSourcesToRemove count] > 0) {
        [powerSourcesToRemove enumerateObjectsUsingBlock:^(NSString * _Nonnull address, NSUInteger idx, BOOL * _Nonnull stop) {
            [_batteryInformation removeObjectForKey:address];
        }];
    }
}

- (void) dispatchBlockForCachedDevice:(CachedDeviceIndex)cachedIndex block:(void (^)(DeviceEventHandler* eventInstanceSelf, IOBluetoothDevice* device))block {
    __block __weak DeviceEventHandler* weakSelf = self;
    IOBluetoothDevice* device = _cachedDevices[cachedIndex];
    
    dispatch_block_t blockToDispatch = dispatch_block_create(0, ^{
        __strong DeviceEventHandler* strongSelf = weakSelf;
        
        block(strongSelf, device);
    });
    
    dispatch_block_notify(blockToDispatch, queue, ^{
        __strong DeviceEventHandler* strongSelf = weakSelf;
        
        [strongSelf modifyScheduledBlock:blockToDispatch mode:ScheduledBlocksRemoveSingle];
    });
    
    [self modifyScheduledBlock:blockToDispatch mode:ScheduledBlocksAddSingle];
    
    dispatch_async(queue, blockToDispatch);
}

- (void) modifyScheduledBlock:(dispatch_block_t)block mode:(ScheduledBlockMode)insMode {
    pthread_mutex_lock(&schedMutex);
    switch (insMode) {
        case ScheduledBlocksAddSingle:
            [scheduledBlocks addObject:block];
            break;
        
        case ScheduledBlocksRemoveSingle:
            [scheduledBlocks removeObject:block];
            break;
            
        case ScheduledBlocksClearAll:
            for (dispatch_block_t currentBlock in scheduledBlocks)
                dispatch_block_cancel(currentBlock);
            
            [scheduledBlocks removeAllObjects];
            break;
    }
    pthread_mutex_unlock(&schedMutex);
}

- (IOReturn) appendPeerToDevice:(CBClassicPeer*)peer forDevice:(IOBluetoothDevice*)device {
    BluetoothDeviceAddress address;
    
    IOReturn ret = IOBluetoothNSStringToDeviceAddress([peer addressString], &address);
    if (ret != kIOReturnSuccess)
        return ret;
    
    [device setAddress:&address];
    [device setPeer:peer];
    
    return ret;
}

- (void) peerUnpaired:(CBClassicPeer *)peer {
    [self removeDevice:peer];
}

- (void) peerPairingCompleted:(CBClassicPeer *)peer withError:(long)error {
    [self addDevice:peer];
}

- (void) peerUpdated:(CBClassicPeer *)peer withResults:(id)results {
    [self updateDevice:peer];
}

- (void) peerConnected:(CBClassicPeer *)peer error:(long)error {
    [self updateDevice:peer];
}

- (void) peerDisconnected:(CBClassicPeer *)peer withError:(long)error {
    [self updateDevice:peer];
}
@end
