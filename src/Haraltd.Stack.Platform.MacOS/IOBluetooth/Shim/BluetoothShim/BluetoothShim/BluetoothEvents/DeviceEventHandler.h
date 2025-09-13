//
//  DeviceEventHandler.h
//  BluetoothShim
//
//  Created by Kendrick on 29/10/25.
//

#import "EventHandler.h"

typedef NSString* PSPropertyName NS_TYPED_ENUM;

typedef enum : NSUInteger {
    CachedDeviceToAdd,
    CachedDeviceToUpdate,
    CachedDeviceToRemove,
} CachedDeviceIndex;

typedef enum : NSUInteger {
    ScheduledBlocksAddSingle,
    ScheduledBlocksRemoveSingle,
    ScheduledBlocksClearAll,
} ScheduledBlockMode;

@interface DeviceEventHandler : NSObject<EventHandler, IOBluetoothCoreBluetoothCoordinatorDelegate>
@end
