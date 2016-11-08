using Android.Content;
using Android.Views;

namespace Echo.Player
{
    //media button action receiver
    [BroadcastReceiver]
    [Android.App.IntentFilter(new[] { Intent.ActionMediaButton })]
    public class EchoMediaButtonReceiver : BroadcastReceiver
    {
        //gets the class name for the component
        public string ComponentName => Class.Name;

        public override void OnReceive(Context context, Intent intent)
        {
            var action = string.Empty;
            if (intent.Action == Intent.ActionMediaButton)
            {
                //the event will fire twice, up and down - we only want to handle the down event
                var key = (KeyEvent) intent.GetParcelableExtra(Intent.ExtraKeyEvent);
                if (key.Action == KeyEventActions.Down)
                {
                    switch (key.KeyCode)
                    {
                        case Keycode.Headsethook:
                        case Keycode.MediaPlayPause:
                            action = EchoPlayerService.ActionTogglePlayback;
                            break;
                        case Keycode.MediaPlay:
                            action = EchoPlayerService.ActionPlay;
                            break;
                        case Keycode.MediaPause:
                            action = EchoPlayerService.ActionPause;
                            break;
                        case Keycode.MediaStop:
                            action = EchoPlayerService.ActionStop;
                            break;
                        case Keycode.MediaNext:
                            action = EchoPlayerService.ActionNext;
                            break;
                        case Keycode.MediaPrevious:
                            action = EchoPlayerService.ActionPrevious;
                            break;
                        default:
                            break;
                    }
                }
            }
            if (string.IsNullOrEmpty(action))
                return;
            var remoteIntent = new Intent(action);
            context.StartService(remoteIntent);
        }
    }
}