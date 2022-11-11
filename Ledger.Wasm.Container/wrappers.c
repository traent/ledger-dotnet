#include <stdint.h>
#include <stdlib.h>

// Generate the declarations for libsodium functions
#define ULONG unsigned long long
#define WRAP(ret, name, types, args) ret name types;
#include "functions.h"

#undef ULONG
#undef WRAP

// Generate the implementations of the wasm_* wrappers to avoid passing
// arguments of type unsigned long long (or, in general, 64-bits arguments).
// They are currently unsupported https://github.com/dotnet/runtime/issues/62085
#define ULONG unsigned int
#define WRAP(ret, name, types, args) ret wasm_ ## name types { return name args; }
#include "functions.h"
