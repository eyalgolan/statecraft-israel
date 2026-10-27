// A macOS trackpad pinch for CameraController (the macOS player only; see its TrackpadMagnification). macOS sends a pinch
// as magnify events (NSEventTypeMagnify), and Unity's player passes them to neither the Input System nor the wheel, so a
// pinch did nothing in the macOS player (owner, 27 Oct 2026) while the browser build zooms on one (the browser sends it
// as a wheel event). An app-local monitor adds each event's magnification to a running sum, which C# takes once a frame.
//
// The monitor only reads: it returns every event unchanged, so the rest of the app still gets it. It is installed on the
// first drain, from CameraController's OnEnable, and never removed; the player installs it once. AppKit runs local
// monitors on the main thread, where Unity's player loop runs on macOS, so the sum needs no lock.
//
// NSEvent is looked up at run time (objc_getClass) rather than linked: Unity links GameAssembly.dylib, which this file is
// compiled into, without AppKit, so a direct [NSEvent ...] fails to link (_OBJC_CLASS_$_NSEvent undefined). The player
// process has AppKit loaded already; were the class missing, the message would go to nil and the sum stay 0.
#import <AppKit/AppKit.h>
#import <objc/runtime.h>

static double statecraftMagnifySum = 0;
static bool statecraftMagnifyListening = false;

extern "C" float StatecraftMagnifyDrain(void)
{
    if (!statecraftMagnifyListening)
    {
        statecraftMagnifyListening = true;
        [objc_getClass("NSEvent") addLocalMonitorForEventsMatchingMask:NSEventMaskMagnify handler:^NSEvent* (NSEvent* event) {
            statecraftMagnifySum += event.magnification;
            return event;
        }];
    }
    double sum = statecraftMagnifySum;
    statecraftMagnifySum = 0;
    return (float)sum;
}
