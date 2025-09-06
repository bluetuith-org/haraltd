using Haraltd.Stack.Obex.Packet;

namespace Haraltd.Stack.Obex.Profiles.Opp;

public static class OppService
{
    public static readonly ObexService Id = new ObexService(
        new Guid("00001105-0000-1000-8000-00805F9B34FB"),
        []
    );
}
