Bvm
---

Bvm is initaially to become a bun version manager on windows, and it later supports deno and nodejs, and also runs on linux x64 and arm64

## How to use

With bvm binary on path

```
# install bun 1.1.45, it will locate at where bvm is at
bvm install -d bun  1.1.45

# if there is no distribution provided, fallback to bun 
bvm use 1.1.45

# Install deno 2.1.6
bvm install -d deno 2.1.6

# Uninstall deno 2.1.6
bvm uninstall -d deno 2.1.6

# Install nodejs 22.13.0
bvm install -d node v22.13.0
```
