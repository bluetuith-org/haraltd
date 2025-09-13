//
//  BluetoothMessageDelegate.h
//  BluetoothShim
//
//  Created by Kendrick on 22/09/25.
//

#import <Foundation/Foundation.h>
#import <BluetoothShim/Result.h>
#import <IOBluetooth/IOBluetooth.h>

typedef enum : NSUInteger {
    kEventActionAdded = 1,
    kEventActionUpdated = 2,
    kEventActionRemoved = 3,
} EventAction;

struct AdapterEventData {
    int Discoverable;
    int Powered;
    int Discovering;
};

struct DeviceBatteryInfo {
    bool modified;
    
    int BatteryPercentSingle;
    int BatteryPercentCombined;
    int BatteryPercentLeft;
    int BatteryPercentRight;
};

struct DeviceEventData {
    struct DeviceBatteryInfo BatteryInfo;
};

@protocol BluetoothEventDelegate <NSObject>
- (void) handleAdapterEvent:(struct AdapterEventData)data;
- (void) handleDeviceEvent:(IOBluetoothDevice*)device eventAction:(EventAction)action additionalData:(struct DeviceEventData)data;
@end

@interface BluetoothEvents : NSObject
+ (ResultWithError<BluetoothEvents*>*) tryInitWithDelegate:(id<BluetoothEventDelegate>)delegate;
@end
