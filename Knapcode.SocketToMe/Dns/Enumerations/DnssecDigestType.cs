namespace Knapcode.SocketToMe.Dns.Enumerations
{
    /// <summary>
    /// Source: http://tools.ietf.org/html/rfc4034
    /// </summary>
    public enum DnssecDigestType
    {
        Reserved = 0,
        Sha1 = 1,

        /// <summary>
        /// Source: https://tools.ietf.org/html/rfc4509
        /// </summary>
        Sha256 = 2
    }
}
