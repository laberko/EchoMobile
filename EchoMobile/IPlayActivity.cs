using System;
using Echo.Player;

namespace Echo
{
    public interface IPlayerInitiator
    {
        EchoPlayerServiceBinder Binder { get; set; }

        void ServiceConnected();
        void OnPlaying(object sender, EventArgs e);
        void OnBuffering(object sender, EventArgs e);
    }
}