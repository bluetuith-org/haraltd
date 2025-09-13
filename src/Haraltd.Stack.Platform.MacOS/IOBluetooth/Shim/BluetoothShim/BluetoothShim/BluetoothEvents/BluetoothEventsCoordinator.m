//
//  EventsHandler.m
//  BluetoothShim
//
//  Created by Kendrick on 22/09/25.
//

#import <Foundation/Foundation.h>
#import <BluetoothShim/BluetoothEvents.h>
#import "BluetoothEventsCoordinator.h"
#import "AdapterEventHandler.h"
#import "DeviceEventHandler.h"

@implementation BluetoothEventsCoordinator {
    NSHashTable* _eventHandlerDelegates;
    
    AdapterEventHandler* adapterEvents;
    DeviceEventHandler* deviceEvents;
}

- (NSError*) tryInit {
    __block NSError* error = nil;
    
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        AdapterEventHandler* ae = [[AdapterEventHandler alloc] init];
        DeviceEventHandler* de = [[DeviceEventHandler alloc] init];
        
        NSArray<id<EventHandler>>* evh = @[ae, de];
        [evh enumerateObjectsUsingBlock:^(id<EventHandler> _Nonnull obj, NSUInteger idx, BOOL * _Nonnull stop) {
            [obj setHandler:self];
            
            error = [obj start];
            if (error != nil) {
                *stop = YES;
            }
        }];
        if (error != nil) {
            NSLog(@"EventHandler: %@", error);
            
            return;
        }
        
        [self setHandlers:ae deviceEvents:de];
    });
    if (error != nil) {
        onceToken = 0;
    }
    
    return error;
}

+ (instancetype) sharedInstance {
    static BluetoothEventsCoordinator* eh;
    if (eh != nil) {
        return eh;
    }
    
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        eh = [[BluetoothEventsCoordinator alloc] init];
    });
    
    return eh;
}

- (bool) addEventDelegate:(id<BluetoothEventDelegate>)delegate {
    [self addDelegate:delegate];
    
    return self != nil;
}

- (bool) removeEventDelegate:(id<BluetoothEventDelegate>)delegate {
    [self removeDelegate:delegate];
    
    return self != nil;
}

- (NSHashTable*) getDelegates {
    return _eventHandlerDelegates;
}

- (instancetype) init {
    self = [super init];
    if (!self) {
        return nil;
    }
    
    _eventHandlerDelegates = [NSHashTable weakObjectsHashTable];
    
    return self;
}

- (void) setHandlers:(AdapterEventHandler*)ae deviceEvents:(DeviceEventHandler*)de{
    adapterEvents = ae;
    deviceEvents = de;
}

- (void) addDelegate:(id<BluetoothEventDelegate>)delegate {
    [_eventHandlerDelegates addObject:delegate];
}

- (void) removeDelegate:(id<BluetoothEventDelegate>)delegate {
    [_eventHandlerDelegates removeObject:delegate];
}
@end
