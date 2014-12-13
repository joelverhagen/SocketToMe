namespace Knapcode.SocketToMe.Dns.Enumerations
{
    /// <summary>
    /// Source: http://tools.ietf.org/html/rfc4034
    /// </summary>
    public enum DnssecAlgorithmType
    {
        ReservedLower = 0,
        RsaMd5 = 1,
        DiffieHellman = 2,
        DsaSha1 = 3,
        EllipticCurve = 4,
        RsaSha1 = 5,
        Indirect = 252,
        PrivateDns = 253,
        PrivateOid = 254,
        ReservedUpper = 255
    }
}
