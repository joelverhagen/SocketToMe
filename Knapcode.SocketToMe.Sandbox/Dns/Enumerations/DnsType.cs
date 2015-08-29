namespace Knapcode.SocketToMe.Sandbox.Dns.Enumerations
{
    /// <summary>
    /// Source: https://www.ietf.org/rfc/rfc1035.txt
    /// </summary>
    public enum DnsType
    {
        A = 1,
        Ns = 2,
        Md = 3,
        Mf = 4,
        Cname = 5,
        Soa = 6,
        Mb = 7,
        Mg = 8,
        Mr = 9,
        Null = 10,
        Wks = 11,
        Ptr = 12,
        Hinfo = 13,
        Minfo = 14,
        Mx = 15,
        Txt = 16,

        /// <summary>
        /// Source: http://tools.ietf.org/html/rfc3596
        /// </summary>
        Aaaa = 28,

        /// <summary>
        /// Source: http://tools.ietf.org/html/rfc4034
        /// </summary>
        Ds = 43,

        /// <summary>
        /// Source: http://tools.ietf.org/html/rfc4034
        /// </summary>
        Rrsig = 46,

        /// <summary>
        /// Source: http://tools.ietf.org/html/rfc4034
        /// </summary>
        Nsec = 47,

        /// <summary>
        /// Source: http://tools.ietf.org/html/rfc4034
        /// </summary>
        DnsKey = 48
    }
}