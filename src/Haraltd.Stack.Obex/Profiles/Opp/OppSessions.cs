using Haraltd.Stack.Base;
using Haraltd.Stack.Obex.Session;

namespace Haraltd.Stack.Obex.Profiles.Opp;

public class OppSessions(IBluetoothStack stack)
    : ObexSessions<OppClient, OppClientProperties, OppServer, OppSubServer, OppServerProperties>(
        stack
    );
