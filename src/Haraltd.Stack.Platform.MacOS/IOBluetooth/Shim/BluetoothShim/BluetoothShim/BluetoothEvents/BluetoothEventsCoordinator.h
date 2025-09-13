//
//  EventsHandler.h
//  BluetoothShim
//
//  Created by Kendrick on 22/09/25.
//

#include <IOKit/ps/IOPowerSources.h>
#import "../Private/Private.h"

CFArrayRef IOPSCopyPowerSourcesByType(Byte a1);

@interface BluetoothEventsCoordinator : NSObject
+ (instancetype) sharedInstance;
- (NSError*) tryInit;
- (bool) addEventDelegate:(id<BluetoothEventDelegate>)delegate;
- (bool) removeEventDelegate:(id<BluetoothEventDelegate>)delegate;
- (NSHashTable*) getDelegates;
@end
