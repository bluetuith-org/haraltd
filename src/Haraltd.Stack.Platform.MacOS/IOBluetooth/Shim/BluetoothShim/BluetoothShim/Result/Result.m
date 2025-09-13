//
//  Result.m
//  BluetoothShim
//
//  Created by Kendrick on 02/10/25.
//

#import <Foundation/Foundation.h>
#import "Result.h"

@implementation ResultWithError
+ (instancetype)initWithResultAndError:(id)result error:(NSError *)err {
    ResultWithError* res = [[ResultWithError alloc] init];
    res.error = err;
    res.result = result;
    
    return res;
}
@end
