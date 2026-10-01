#import <AVFoundation/AVFoundation.h>
#include <dispatch/dispatch.h>
#include "mac_permissions.h"

bool EnsureMicrophoneAccess()
{
	if (@available(macOS 10.14, *)) {
		AVAuthorizationStatus status = [AVCaptureDevice authorizationStatusForMediaType:AVMediaTypeAudio];
		if (status == AVAuthorizationStatusAuthorized) {
			return true;
		}
		if (status != AVAuthorizationStatusNotDetermined) {
			return false;
		}

		// Show the system prompt exactly once and wait for the user's answer, so the audio
		// device isn't initialized (and the prompt re-triggered) while it is still pending.
		__block bool granted = false;
		dispatch_semaphore_t sem = dispatch_semaphore_create(0);
		[AVCaptureDevice requestAccessForMediaType:AVMediaTypeAudio completionHandler:^(BOOL ok) {
			granted = ok;
			dispatch_semaphore_signal(sem);
		}];
		dispatch_semaphore_wait(sem, DISPATCH_TIME_FOREVER);
		return granted;
	}
	return true;
}
