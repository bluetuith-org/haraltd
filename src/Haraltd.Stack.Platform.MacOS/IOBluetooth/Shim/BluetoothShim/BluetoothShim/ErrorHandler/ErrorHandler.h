//
//  ErrorHandler.h
//  BluetoothShim
//
//  Created by Kendrick on 02/10/25.
//

#import <Foundation/Foundation.h>

@interface ErrorHandler : NSObject
+ (NSError *) executeAndGetError:(const char *)funcName block:(__attribute__((noescape)) NSError*(^)(void))tryBlock;
+ (NSError *) createError:(NSString*)errorName reason:(NSString*)briefReason funcName:(const char *)fName;
@end
