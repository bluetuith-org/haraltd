//
//  EventHandler.h
//  BluetoothShim
//
//  Created by Kendrick on 29/10/25.
//

#import "BluetoothEventsCoordinator.h"

@protocol EventHandler <NSObject>
- (void) setHandler:(BluetoothEventsCoordinator*)handler;
- (NSError *) start;
@end
