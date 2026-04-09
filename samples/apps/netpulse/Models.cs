namespace NetPulse;

// TCP connection states from MIB_TCP_STATE
enum TcpState : uint
{
    Closed = 1,
    Listen = 2,
    SynSent = 3,
    SynReceived = 4,
    Established = 5,
    FinWait1 = 6,
    FinWait2 = 7,
    CloseWait = 8,
    Closing = 9,
    LastAck = 10,
    TimeWait = 11,
    DeleteTcb = 12,
}

record TcpConn(
    string LocalAddr,
    ushort LocalPort,
    string RemoteAddr,
    ushort RemotePort,
    TcpState State,
    uint Pid)
{
    public string Key => $"{LocalAddr}:{LocalPort}-{RemoteAddr}:{RemotePort}";

    public string ShortRemote => RemotePort > 0
        ? $"{RemoteAddr}:{RemotePort}"
        : RemoteAddr;
}

record UdpEndpoint(
    string LocalAddr,
    ushort LocalPort,
    uint Pid);

record IfaceRate(
    string Name,
    double InBytesPerSec,
    double OutBytesPerSec,
    ulong Speed);

record TrafficSample(
    long TimestampMs,
    double InBytesPerSec,
    double OutBytesPerSec);

record SparklineEntry(string Key, string Label, int[] StateHistory);
