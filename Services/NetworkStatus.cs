namespace RailwayErp.MvvmDemo.Services;

public interface INetworkStatus
{
    bool IsOnline { get; }

    /// <summary>Примусовий офлайн-режим (для демонстрації та скріншотів на емуляторі).</summary>
    bool ForceOffline { get; set; }

    event EventHandler? Changed;
}

public class NetworkStatus : INetworkStatus
{
    private bool _forceOffline;

    public NetworkStatus()
    {
        Connectivity.Current.ConnectivityChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? Changed;

    public bool ForceOffline
    {
        get => _forceOffline;
        set
        {
            if (_forceOffline == value)
                return;

            _forceOffline = value;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool IsOnline =>
        !_forceOffline && Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
}
