//
//  ErrorHandler.m
//  BluetoothShim
//
//  Created by Kendrick on 02/10/25.
//

#import <Foundation/Foundation.h>
#import "ErrorHandler.h"

@implementation ErrorHandler
+ (NSError *) executeAndGetError:(const char *)funcName block:(__attribute__((noescape)) NSError*(^)(void))tryBlock {
    @try {
        return tryBlock();
    } @catch (NSException *exception) {
        return [self createError:exception.name reason:exception.reason funcName:funcName];
    }
}

+ (NSError *) createError:(NSString*)errorName reason:(NSString*)briefReason funcName:(const char *)fName {
    return [[NSError alloc] initWithDomain:@"com.haraltd.InternalError" code:1 userInfo:@{
        @"ErrorName": errorName,
        NSLocalizedDescriptionKey: [[NSString alloc] initWithFormat:@"%s: %@", fName, briefReason],
    }];
}
@end
