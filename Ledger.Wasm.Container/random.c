#include <stdint.h>
#include <stdlib.h>

typedef void random_source_fn(void* buffer, int len);
static random_source_fn *random_source;

void wasm_set_random_source(random_source_fn fn)
{
    random_source = fn;
}

// libsodium expects arc4random_buf and arc4random to be available, so we must
// provide them in the WASM environment
void arc4random_buf(void* buffer, size_t len)
{
    return random_source(buffer, len);
}

uint32_t arc4random(void)
{
    uint32_t v;

    arc4random_buf(&v, sizeof v);

    return v;
}
