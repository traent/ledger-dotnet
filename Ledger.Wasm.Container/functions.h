WRAP(int, sodium_init, (void), ())

WRAP(int, sodium_pad, (
    size_t *padded_buflen_p,
    unsigned char *buf,
    size_t unpadded_buflen,
    size_t blocksize,
    size_t max_buflen
    ), (padded_buflen_p, buf, unpadded_buflen, blocksize, max_buflen)
)
WRAP(int, sodium_unpad, (
    size_t *unpadded_buflen_p,
    const unsigned char *buf,
    size_t padded_buflen,
    size_t blocksize
    ), (unpadded_buflen_p, buf, padded_buflen, blocksize)
)

WRAP(int, crypto_aead_chacha20poly1305_ietf_decrypt_detached, (
    unsigned char *m,
    unsigned char *nsec,
    const unsigned char *c,
    ULONG clen,
    const unsigned char *mac,
    const unsigned char *ad,
    ULONG adlen,
    const unsigned char *npub,
    const unsigned char *k
    ), (m, nsec, c, clen, mac, ad, adlen, npub, k)
)
WRAP(int, crypto_aead_chacha20poly1305_ietf_encrypt_detached, (
    unsigned char *c,
    unsigned char *mac,
    unsigned long long *maclen_p,
    const unsigned char *m,
    ULONG mlen,
    const unsigned char *ad,
    ULONG adlen,
    const unsigned char *nsec,
    const unsigned char *npub,
    const unsigned char *k
    ), (c, mac, maclen_p, m, mlen, ad, adlen, nsec, npub, k)
)

WRAP(int, crypto_box_curve25519xchacha20poly1305_keypair, (
    unsigned char *pk,
    unsigned char *sk
    ), (pk, sk)
)
WRAP(int, crypto_box_curve25519xchacha20poly1305_beforenm, (
    unsigned char *k,
    const unsigned char *pk,
    const unsigned char *sk
    ), (k, pk, sk)
)

WRAP(int, crypto_sign_detached, (
    unsigned char *sig,
    unsigned long long *siglen_p,
    const unsigned char *m,
    ULONG mlen,
    const unsigned char *sk
    ), (sig, siglen_p, m, mlen, sk)
)
WRAP(int, crypto_sign_ed25519_pk_to_curve25519, (
    unsigned char *curve25519_pk,
    const unsigned char *ed25519_pk
    ), (curve25519_pk, ed25519_pk)
)
WRAP(int, crypto_sign_ed25519_sk_to_curve25519, (
    unsigned char *curve25519_sk,
    const unsigned char *ed25519_sk
    ), (curve25519_sk, ed25519_sk)
)
WRAP(int, crypto_sign_keypair, (
    unsigned char *pk,
    unsigned char *sk
    ), (pk, sk)
)
WRAP(int, crypto_sign_verify_detached, (
    const unsigned char *sig,
    const unsigned char *m,
    ULONG mlen,
    const unsigned char *pk
    ), (sig, m, mlen, pk)
)
