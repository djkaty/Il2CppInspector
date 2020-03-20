These headers were built using roughly the following approach:

- Paste the following headers together in this order (skipping any missing ones):

		il2cpp-config.h

		blob.h
		il2cpp-blob.h

		il2cpp-metadata.h
		metadata.h

		il2cpp-api-types.h
		types.h
		il2cpp-string-types.h

		il2cpp-runtime-metadata.h

		class-internals.h
		il2cpp-class-internals.h

		object-internals.h
		il2cpp-object-internals.h

- Remove any lines starting with `#include "` or `#pragma once`
- Manually fix the header until it compiles under `g++ header.h -o /dev/null`
	- This usually just means removing or commenting out offending constructs
- Preprocess the header using `grep -v '#include' HEADER.h | cpp -xc -P -E - > HEADER.i` to simplify it
- Manually fix the header until IDA will import it without errors
- Split the `Il2CppClass` structure (to allow `static_fields` and `vtable` to be typed per-class), add padding to `cctor_thread` if necessary, and expand zero-length structs
- Import the final `.i` file into this directory and specify `Build Action -> Embedded Resource`
