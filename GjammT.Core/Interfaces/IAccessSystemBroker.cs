namespace GjammT.Core.Interfaces;

public interface IAccessSystemBroker : IGjammT
{
    string Heartbeat();

    string KO();

    string MASK();
}