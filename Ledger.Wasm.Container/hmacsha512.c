#include <stdint.h>
#include <stdlib.h>

typedef struct crypto_hash_sha512_state {
    uint64_t state[8];
    uint64_t count[2];
    uint8_t  buf[128];
} crypto_hash_sha512_state;

typedef struct crypto_auth_hmacsha512_state {
    crypto_hash_sha512_state ictx;
    crypto_hash_sha512_state octx;
} crypto_auth_hmacsha512_state;

int crypto_auth_hmacsha512_init(crypto_auth_hmacsha512_state *state,
                                const unsigned char *key,
                                size_t keylen);

int crypto_auth_hmacsha512_update(crypto_auth_hmacsha512_state *state,
                                  const unsigned char *in,
                                  unsigned long long inlen);

int crypto_auth_hmacsha512_final(crypto_auth_hmacsha512_state *state,
                                 unsigned char *out);

// A utility function that performs one-shot HMACSHA512
int
wasm_crypto_auth_hmacsha512(
    unsigned char *out,
    const unsigned char *in,
    unsigned int inlen,
    const unsigned char *k,
    unsigned int klen)
{
    crypto_auth_hmacsha512_state state;

    crypto_auth_hmacsha512_init(&state, k, klen);
    crypto_auth_hmacsha512_update(&state, in, inlen);
    crypto_auth_hmacsha512_final(&state, out);

    return 0;
}
