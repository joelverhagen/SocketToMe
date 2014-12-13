namespace Knapcode.SocketToMe.Dns.Enumerations
{
    /// <summary>
    /// Source: https://tools.ietf.org/html/draft-ietf-dnsind-udp-size-02
    /// </summary>
    public enum RequestCode
    {
        Size512 = 0,
        Size768 = 1,
        Size1280 = 2,
        Size1920 = 3,
        Size3200 = 4,
        Size4800 = 5,
        Size8000 = 6,
        Size12000 = 7,
        Size20000 = 8,
        Size30000 = 9,
        Size50000 = 10,
        Reserved = 11
    }
}
