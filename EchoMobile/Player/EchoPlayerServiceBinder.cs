using Android.OS;

namespace Echo.Player
{
    public class EchoPlayerServiceBinder : Binder
    {
        private readonly EchoPlayerService _service;
        public EchoPlayerServiceBinder(EchoPlayerService service)
        {
            _service = service;
        }
        public EchoPlayerService GetMediaPlayerService()
        {
            return _service;
        }
    }
}