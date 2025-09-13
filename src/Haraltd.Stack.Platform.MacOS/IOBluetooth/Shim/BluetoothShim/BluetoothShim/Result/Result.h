//
//  Result.h
//  BluetoothShim
//
//  Created by Kendrick on 02/10/25.
//

#import <Foundation/Foundation.h>

@interface ResultWithError<T> : NSObject
@property (nonatomic, strong) NSError* _Nullable error;
@property (nonatomic, strong) T _Nullable result;

+ (instancetype _Nonnull) initWithResultAndError:(T _Nullable)result error:(NSError* _Nullable)err;
@end
