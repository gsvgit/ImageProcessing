# Brahma.FSharp examples

Few example how to utilize GPGPU in F# code using [Brahma.FSharp](https://github.com/YaccConstructor/Brahma.FSharp).

- [Image processing](src/ImageProcessing)
- [Matrix multiplication](src/MatrixMultiplication) 

---

## Builds


GitHub Actions |
:---: |
[![GitHub Actions](https://github.com/gsvgit/ImageProcessing/workflows/Build%20master/badge.svg)](https://github.com/gsvgit/ImageProcessing/actions?query=branch%3Amaster) |
[![Build History](https://buildstats.info/github/chart/gsvgit/ImageProcessing)](https://github.com/gsvgit/ImageProcessing/actions?query=branch%3Amaster) |

---

### Developing

Make sure the following **requirements** are installed on your system:

- [dotnet SDK 9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) or higher
- OpenCL-compatible device with respective driver installed.

---

### Building


```sh
dotnet build -c Release
```