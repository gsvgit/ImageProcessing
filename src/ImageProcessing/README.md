## Image Processing

Image convolution with configurable filters on CPU (sequential / parallel) and GPU via OpenCL. Includes a `MailboxProcessor`-based streaming pipeline for batch processing.

### Backends

| Implementation | Function | Parallelism |
|---|---|---|
| CPU Sequential | `applyFilter` | Single-threaded pixel loop |
| CPU Parallel (per-pixel) | `applyFilterCpuParallel` | `Array.Parallel.iter` over flat pixel array |
| CPU Parallel (per-row) | `applyFilterCpuParallelRows` | `Array.Parallel.iter` over rows, sequential within each row |
| GPU (any OpenCL device) | `applyFiltersGPU` | OpenCL kernel, configurable local work size |

**Streaming mode**: a `MailboxProcessor`-based pipeline that loads images from a directory, distributes them across multiple filter workers (each potentially on a different platform), and saves results — all concurrently.

**CLI**: Argu-based argument parser supports `--input`, `--output`, `--platform` (6 backends), `--work-group-size`, and `--workers` for streaming.

---

## Benchmarks

The `benchmarks/ImageProcessing.Benchmarks/` project uses **BenchmarkDotNet** to measure filter processing times across all available backends — CPU sequential, CPU parallel (per-pixel and per-row), and GPU (POCL, Nvidia, Intel GPU) — for square images from 100×100 up to 8000×8000 pixels.

### Benchmark classes

| Class | Device param | Extra params |
|---|---|---|
| `CpuFilterBench` | `CPUSequential`, `CPUParallel`, `CPUParallelRows` | — |
| `GpuFilterBench` | `POCL`, `Nvidia`, `IntelGPU` | `LWS`: 8, 16, 32, 64, 128, 256 |

Common parameter across all classes:
- **Size** — image side in pixels: 100, 200, 500, 1000, 2000, 4000, 8000

### Design

- **Image generation**: each benchmark generates a random square image (deterministic seed `Random 42`) in `[GlobalSetup]` — excluded from measurement
- **Filter**: all benchmarks use `gaussianBlurKernel` (5×5 normalized) — a module-level constant, not recreated per invocation
- **Measurement**: the `[Benchmark]` method applies exactly one filter pass and discards the result; only the processing time is captured
- **GPU setup**: `ClContext` and GPU applier are created once in `[GlobalSetup]`; the device name is printed at startup
- **No redundant combinations**: `LWS` is only parameterized for GPU benchmarks — CPU benchmarks have zero useless LWS configurations

### How to run

```bash
# Full run (interactive menu selects CPU or GPU benchmarks):
dotnet run -c Release --project benchmarks/ImageProcessing.Benchmarks

# Quick smoke test (CPU only, ShortRun):
dotnet run -c Release --project benchmarks/ImageProcessing.Benchmarks -- --job short --filter '*CpuFilterBench*'

# GPU benchmarks:
dotnet run -c Release --project benchmarks/ImageProcessing.Benchmarks -- --filter '*GpuFilterBench*'

# CPU benchmarks:
dotnet run -c Release --project benchmarks/ImageProcessing.Benchmarks -- --filter '*CPUParallel*'
```

BenchmarkDotNet passes remaining CLI arguments (like `--filter`, `--job`, `--stopOnFirstError`) through to its own parser. Results are exported as CSV, Markdown, and HTML to `BenchmarkDotNet.Artifacts/results/`.

### Analysis script

The Python script [`benchmarks/analyze_benchmarks.py`](../benchmarks/analyze_benchmarks.py) reads the CSV results and generates two comparison plots:

- **GPU work-group size comparison** (left): compares Intel UHD Graphics 620, NVIDIA GeForce MX150, and POCL (CPU OpenCL) across all work-group sizes (8–256) at the largest image (8000×8000)
- **CPU vs best GPU configurations** (right): compares all three CPU variants (sequential, pixel-parallel, rows-parallel) against the best-performing LWS per GPU device across all image sizes

```bash
python benchmarks/analyze_benchmarks.py
```

Example of plot:
![Benchmark comparison](../../figures/benchmark_comparison.svg)


