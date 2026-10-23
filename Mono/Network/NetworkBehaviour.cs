namespace Mono.Network;

public abstract class NetworkBehaviour
{
    public uint NetworkId;
    public virtual void ServerTick() {}
    public virtual void ClientTick() {}
}