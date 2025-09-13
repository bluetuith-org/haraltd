//
//  BluetoothMessage.m
//  BluetoothShim
//
//  Created by Kendrick on 22/09/25.
//

#import <Foundation/Foundation.h>
#import <BluetoothShim/BluetoothEvents.h>
#import "BluetoothEventsCoordinator.h"

@implementation BluetoothEvents
+ (ResultWithError<BluetoothEvents *> *) tryInitWithDelegate:(id<BluetoothEventDelegate>)delegate {
    ResultWithError* res = [ResultWithError initWithResultAndError:nil error:nil];
    
    BluetoothEvents* be = [[BluetoothEvents alloc] init];
    if (be == nil) {
        res.error = [NSError errorWithDomain:NSCocoaErrorDomain code:1 userInfo:@{@"error": @"BluetoothEvents object is null"}];
        return res;
    }
    
    [[BluetoothEventsCoordinator sharedInstance] addEventDelegate:delegate];
    res.result = be;
    
    return res;
}
@end
