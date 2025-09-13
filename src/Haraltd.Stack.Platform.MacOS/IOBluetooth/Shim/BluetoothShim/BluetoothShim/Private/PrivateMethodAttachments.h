//
//  SwizzledWorkarounds.h
//  BluetoothShim
//
//  Created by Kendrick on 29/10/25.
//
@import SwiftHook;

@interface PrivateMethodAttachments : NSObject
+ (OCToken *) attachForUnknownNonNullDeviceGlobally:(NSError**)error;
@end
