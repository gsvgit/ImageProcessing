# Brahma.FSharp GPGPU Examples: Image Processing & Matrix Multiplication

GitHub Actions |
:---: |
[![GitHub Actions](https://github.com/gsvgit/ImageProcessing/workflows/Build%20master/badge.svg)](https://github.com/gsvgit/ImageProcessing/actions?query=branch%3Amaster) |
[![Build History](https://buildstats.info/github/chart/gsvgit/ImageProcessing)](https://github.com/gsvgit/ImageProcessing/actions?query=branch%3Amaster) |

---

This repository contains practical, educational examples of **General-Purpose computing on Graphics Processing Units (GPGPU)** using the **F#** programming language. It serves as a hands-on guide to leveraging the [**Brahma.FSharp**](https://github.com/YaccConstructor/Brahma.FSharp) library for writing parallel code that executes on OpenCL-compatible devices like GPUs.

The primary goal is to demonstrate how to accelerate common computational problems by offloading them from the CPU to the GPU, showcasing both the performance potential and the implementation patterns in F#.

Few example how to utilize GPGPU in F# code using [Brahma.FSharp](https://github.com/YaccConstructor/Brahma.FSharp).

## ✨ Features

This project currently includes two classic GPGPU examples:

1.  **Image Convolution**: Applies various filters (like blur, sharpen, edge detection) to images. This operation is inherently parallel, as each output pixel can be computed independently from its neighbors, making it an ideal candidate for GPU acceleration. (Located in [`src/ImageProcessing/`](src/ImageProcessing)).
2.  **Matrix Multiplication**: Implements the multiplication of two large matrices on the GPU. This is a fundamental operation in many scientific and engineering domains and perfectly illustrates data-parallel computing. (Located in [`src/MatrixMultiplication/`](src/MatrixMultiplication) ).

Both examples are designed to be simple to understand while demonstrating core concepts like kernel definition, memory management, and execution on a compute device.

---

## 📁 Repository Structure

The project is organized for clarity and ease of navigation:

*   `src/`: Contains all source code.
    *   `ImageProcessing/`: The image convolution example and related logic.
    *   `MatrixMultiplication/`: The matrix multiplication implementation.
*   `tests/`: Unit tests for the examples, ensuring correctness.
    *   `ImageProcessing.Tests/`
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

### Installation & Build

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/gsvgit/ImageProcessing.git
    cd ImageProcessing
    ```

2.  **Build the project:**
    This command compiles the code and restores any necessary NuGet packages.
    ```bash
    dotnet build -c Release
    ```