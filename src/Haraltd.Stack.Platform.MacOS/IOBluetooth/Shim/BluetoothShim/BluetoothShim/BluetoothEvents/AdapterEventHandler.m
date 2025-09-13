//
//  AdapterEventHandler.m
//  BluetoothShim
//
//  Created by Kendrick on 12/10/25.
//

#import <Foundation/Foundation.h>
#import <BluetoothShim/BluetoothEvents.h>
#import "BluetoothEventsCoordinator.h"
#import "AdapterEventHandler.h"

static void* const AdapterEventContext = (void *)&AdapterEventContext;
typedef bool (^PropertyAction)(struct AdapterEventData*, id value);

@implementation AdapterEventHandler {
    CBClassicManager* manager;
    
    __weak BluetoothEventsCoordinator* eh;
    NSDictionary<NSString*, PropertyAction>* adapterPropertyList;
    
    dispatch_queue_t queue;
}

- (void) dealloc {
    [adapterPropertyList enumerateKeysAndObjectsUsingBlock:^(NSString * _Nonnull key, PropertyAction  _Nonnull obj, BOOL * _Nonnull stop) {
        [manager removeObserver:self forKeyPath:key context:AdapterEventContext];
    }];
}

- (instancetype) init {
    self = [super init];
    if (!self) {
        return nil;
    }
    
    //    @"localName" : nil,
    //    @"inquiryState" : nil,
    
    //    @"connectable" : nil
    adapterPropertyList = @{
        @"powerState" : ^bool(struct AdapterEventData * data, id value) {
            data->Powered = [value boolValue] + 4;
            return true;
        },
        @"discoverable" : ^bool(struct AdapterEventData * data, id value) {
            data->Discoverable = [value boolValue] + 4;
            return true;
        },
    };
    
    dispatch_queue_attr_t queue_attr = dispatch_queue_attr_make_with_autorelease_frequency(0, DISPATCH_AUTORELEASE_FREQUENCY_WORK_ITEM);
    queue = dispatch_queue_create("com.haraltd.events.AdapterQueue", queue_attr);
    
    return self;
}

- (void) setHandler:(BluetoothEventsCoordinator*)handler {
    eh = handler;
}

- (NSError *) start {
    NSError* error;
    
    @try {
        manager = [[IOBluetoothCoreBluetoothCoordinator sharedInstance] classicManager];
        
        [adapterPropertyList enumerateKeysAndObjectsUsingBlock:^(NSString * _Nonnull key, PropertyAction  _Nonnull obj, BOOL * _Nonnull stop) {
            [manager addObserver:self forKeyPath:key
                         options:NSKeyValueObservingOptionOld|NSKeyValueObservingOptionNew context:AdapterEventContext];
        }];
    } @catch (NSException *exception) {
        error = [NSError errorWithDomain:[exception name] code:1 userInfo:[exception userInfo]];
    }

    return error;
}

- (void) observeValueForKeyPath:(NSString *)keyPath ofObject:(id)object change:(NSDictionary<NSKeyValueChangeKey,id> *)change context:(void *)context {
    id old = change[NSKeyValueChangeOldKey];
    id new = change[NSKeyValueChangeNewKey];
    
    if (old == new)
        return;
    
    PropertyAction action = adapterPropertyList[keyPath];
    if (!action)
        return;
    
    __block struct AdapterEventData data = {};
    if (!action(&data, new))
        return;

    dispatch_async(queue, ^{
        for (id<BluetoothEventDelegate> delegate in [self->eh getDelegates]) {
            [delegate handleAdapterEvent:data];
        }
    });
}
@end
