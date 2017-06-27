using Android.App;
using Android.Content;
using Android.Media;
using Android.Views;

namespace Echo.Player
{
    //media action receiver
    [BroadcastReceiver(Enabled = true, Exported = false)]
    [IntentFilter(new[] { Intent.ActionMediaButton, Intent.ActionHeadsetPlug, AudioManager.ActionAudioBecomingNoisy })]
    public class EchoMediaReceiver : BroadcastReceiver
    {
        //gets the class name for the component
        public string ComponentName => Class.Name;

        public override void OnReceive(Context context, Intent intent)
        {
            var action = string.Empty;
            switch (intent.Action)
            {
                case Intent.ActionMediaButton:
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
                    break;
                case Intent.ActionHeadsetPlug:
                case AudioManager.ActionAudioBecomingNoisy:
                    action = EchoPlayerService.ActionPause;
                    break;
            }
            if (string.IsNullOrEmpty(action))
                return;
            var remoteIntent = new Intent(action, null, context, typeof(EchoPlayerService));
            context.StartService(remoteIntent);
        }
    }
}