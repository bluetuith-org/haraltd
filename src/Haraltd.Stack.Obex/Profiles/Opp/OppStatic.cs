using Haraltd.Stack.Obex.Packet;

namespace Haraltd.Stack.Obex.Profiles.Opp;

public static class OppStatic
{
    internal const ushort MinimumClientPacketSize = 256;

    internal static readonly ObexService Id = new OppService();
}
