namespace Traent.Ledger.Crypto {
    public interface ICryptoProvider {
        IOneShotHash CreateOneShotHash(string? algorithm);
        ISignatureAlgorithm CreateSignatureAlgorithm(string? algorithm);
    }
}
