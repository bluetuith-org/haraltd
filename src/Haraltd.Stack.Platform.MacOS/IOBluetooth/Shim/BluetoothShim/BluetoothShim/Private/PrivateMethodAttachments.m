//
//  PrivateMethodAttachments.m
//  BluetoothShim
//
//  Created by Kendrick on 29/10/25.
//

#import "PrivateMethodAttachments.h"
#import "CBClassicPeer.h"

@implementation PrivateMethodAttachments
+ (OCToken *) attachForUnknownNonNullDeviceGlobally:(NSError**)error {
    return [NSClassFromString(@"CBClassicManager") sh_hookInsteadWithSelector:NSSelectorFromString(@"classicPeerWithInfo:")
                                                                      closure:^CBClassicPeer* (CBClassicPeer*(^original)(id obj, SEL sel, NSDictionary* dict), id object, SEL selector, NSDictionary* dictionary) {
         CBClassicPeer* peer = original(object, selector, dictionary);
         if ([[peer addressString] length] == 0)
             return nil;
         
         return peer;
    } error:error];
}
@end
