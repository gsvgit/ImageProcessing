# Brahma.FSharp GPGPU Examples: Image Processing & Matrix Multiplication

GitHub Actions |
:---: |
[![GitHub Actions](https://github.com/gsvgit/ImageProcessing/workflows/Build%20master/badge.svg)](https://github.com/gsvgit/ImageProcessing/actions?query=branch%3Amaster) |
[![Build History](https://buildstats.info/github/chart/gsvgit/ImageProcessing)](https://github.com/gsvgit/ImageProcessing/actions?query=branch%3Amaster) |

---

Practical GPGPU examples in **F#** using the [**Brahma.FSharp**](https://github.com/YaccConstructor/Brahma.FSharp) library for OpenCL-accelerated computing.

## Projects

| Project | Source | README |
|---|---|---|
| **Image Convolution** | [`src/ImageProcessing/`](src/ImageProcessing) | [details](src/ImageProcessing/README.md) — filters, CPU/GPU backends, streaming pipeline, benchmarks |
| **Matrix Multiplication** | [`src/MatrixMultiplication/`](src/MatrixMultiplication) | [details](src/MatrixMultiplication/README.md) — progressive GPU kernels K0–K4, benchmarks |

## 📁 Repository Structure

The project is organized for clarity and ease of navigation:

*   `src/`: Contains all source code.
    *   `ImageProcessing/`: `Image` type, filter kernels, CPU/GPU convolution, `MailboxProcessor` streaming pipeline, CLI entry point.
    *   `MatrixMultiplication/`: Matrix multiplication kernels (K0–K4) and CLI entry point.
*   `tests/`: Unit tests for the examples, ensuring correctness.
    *   `ImageProcessing.Tests/`: Xunit tests for all CPU and GPU filter variants.
    *   `MatrixMultiplication.Tests/`: Xunit tests for matrix multiplication kernels.
*   `benchmarks/`: Performance benchmarks.
    *   `MatrixMultiplication.Benchmarks/`: Benchmarks for matrix multiplication kernels K0–K4.
    *   `ImageProcessing.Benchmarks/`: Benchmarks for image convolution on all CPU and GPU backends.
*   `.github/workflows/`: GitHub Actions CI/CD pipelines for automated building and testing.


## 🚀 Getting Started

Follow these instructions to get the project up and running on your local machine for development and experimentation.

### Prerequisites

Before you begin, ensure you have the following installed:

*   **[.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)** or higher.
*   **Option A (Recommended for GPU acceleration):** An **OpenCL-compatible device** (e.g., a discrete or integrated GPU) with the **respective vendor driver** installed. (e.g., NVIDIA drivers for NVIDIA GPUs, ROCm or AMD drivers for AMD GPUs, or Intel OpenCL runtime for Intel GPUs/CPUs).
*   **Option B (CPU fallback - great for testing/learning):** If you don't have a GPU or want to experiment on CPU first, install **[POCL](http://portablecl.org/) (Portable Computing Language)**. POCL is an open-source OpenCL implementation that runs on CPUs, allowing you to run all examples without dedicated graphics hardware.
    *   **On Ubuntu/Debian:** `sudo apt install pocl-opencl-icd`
    *   Check the [official POCL installation guide](https://portablecl.org/docs/html/install.html) for installation options.

### Build

```bash
git clone https://github.com/gsvgit/ImageProcessing.git
cd ImageProcessing
dotnet build -c Release
```

## Benchmarks

Both projects include BenchmarkDotNet performance benchmarks. See each sub-project's README for details, run instructions, and analysis figures.

### Analysis script

The Python script [`benchmarks/analyze_benchmarks.py`](../benchmarks/analyze_benchmarks.py) can be used to plot benchmarks results. For retails look at sub-project's README.

**Analysis requirements:** `pandas`, `matplotlib`, `numpy`
```bash
pip install pandas matplotlib numpy
```