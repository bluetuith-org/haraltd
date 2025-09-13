//
//  Base.m
//  BluetoothShim
//
//  Created by Kendrick on 22/09/25.
//

#import <Foundation/Foundation.h>
#import <BluetoothShim/BluetoothShim.h>
#import "BluetoothEvents/BluetoothEventsCoordinator.h"
#import "Private/Private.h"
#import "ErrorHandler/ErrorHandler.h"

@implementation BluetoothShim
+ (ResultWithError<NSNumber *> *) getBluetoothPermissionStatus {
    __block long tccStatus = 0;
    
    NSError* error = [ErrorHandler executeAndGetError:__func__ block:^NSError *{
        tccStatus = [[IOBluetoothCoreBluetoothCoordinator sharedInstance] getTCCApprovalStatus];
        return nil;
    }];
    
    return [ResultWithError initWithResultAndError:[NSNumber numberWithBool:tccStatus > 0] error:error];
}

+ (NSError*) tryInitializeCoordinator {
    NSError* error;
    
    [PrivateMethodAttachments attachForUnknownNonNullDeviceGlobally:&error];
    if (error == nil)
        error = [[BluetoothEventsCoordinator sharedInstance] tryInit];
    
    return error;
}

+ (ResultWithError<NSArray<IOBluetoothDevice *> *>*) getPairedDevices {
    __block NSArray* devices;
    
    NSError* error = [ErrorHandler executeAndGetError:__func__ block:^NSError *{
        devices = getPairedDevices();
        return nil;
    }];
    
    return [ResultWithError initWithResultAndError:devices error:error];
}

+ (NSString *) getHostControllerAddressString {
    static char* emptyAddress = "00:00:00:00:00:00";
    NSString* ns = [NSString stringWithUTF8String:emptyAddress];
    
    io_service_t service = IOServiceGetMatchingService(kIOMainPortDefault, IOServiceNameMatching("bluetooth"));
    if (service) {
        CFDataRef macaddr = IORegistryEntryCreateCFProperty(service, CFSTR("local-mac-address"), kCFAllocatorDefault, 0);
        if (macaddr != nil) {
            if (CFDataGetLength(macaddr) == 6) {
                unsigned char addr[6];
                
                CFDataGetBytes(macaddr, CFRangeMake(0, 6), addr);
                
                ns = [[NSString alloc] initWithFormat:@"%02x:%02x:%02x:%02x:%02x:%02x",
                                addr[0], addr[1], addr[2], addr[3], addr[4], addr[5]];
            }
            
            CFRelease(macaddr);
        }
    }
    
    
    return ns;
}

+ (IOBluetoothDevice*) getKnownDevice:(NSString*)address isKnown:(bool*)known  {
    IOBluetoothDevice* device = [IOBluetoothDevice deviceWithAddressString:address];
    *known = [device classicPeer] != nil && [[[device classicPeer] addressString] length] != 0;
        
    return device;
}

+ (IOBluetoothDevice*) setNilPeripheral:(IOBluetoothDevice*)device {
    [device setPeripheral:nil];
    
    return device;
}

+ (void) removeKnownDevice:(IOBluetoothDevice*)device {
    [device remove];
    purgeDeviceFromCache(device);
}
@end
