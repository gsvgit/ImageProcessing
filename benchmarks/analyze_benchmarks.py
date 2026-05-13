import pandas as pd
import matplotlib.pyplot as plt
import numpy as np
from pathlib import Path

artifacts_dir = Path(__file__).resolve().parent.parent / "BenchmarkDotNet.Artifacts" / "results"
gpu_csv = artifacts_dir / "ImageProcessingBenchmarks.GpuFilterBench-report.csv"
cpu_csv = artifacts_dir / "ImageProcessingBenchmarks.CpuFilterBench-report.csv"
output_svg = Path(__file__).resolve().parent / "benchmark_comparison.svg"

def parse_time_ms(val):
    if pd.isna(val) or str(val).strip() == 'NA':
        return np.nan
    s = str(val).replace(',', '').replace('"', '').replace(' ', '')
    if 'μs' in s:
        return float(s.replace('μs', '')) / 1000
    if 'ms' in s:
        return float(s.replace('ms', ''))
    if 'ns' in s:
        return float(s.replace('ns', '')) / 1e6
    return float(s)

gpu = pd.read_csv(gpu_csv, skipinitialspace=True)
cpu = pd.read_csv(cpu_csv, skipinitialspace=True)

gpu = gpu[gpu['Job'] == 'DefaultJob'].copy()
gpu['Time_ms'] = gpu['Mean'].apply(parse_time_ms)
gpu['LWS'] = gpu['LWS'].astype(int)
gpu['Size'] = gpu['Size'].astype(int)

cpu = cpu.copy()
cpu['Time_ms'] = cpu['Mean'].apply(parse_time_ms)
cpu['Size'] = cpu['Size'].astype(int)

wgs_values = sorted(gpu['LWS'].unique())
size_values = sorted(gpu['Size'].unique())
device_names = ['IntelGPU', 'Nvidia', 'POCL']
cpu_names = ['CPUSequential', 'CPUParallel', 'CPUParallelRows']

colors_plot1 = {'IntelGPU': '#4C72B0', 'Nvidia': '#DD8452', 'POCL': '#55A868'}

fig, axes = plt.subplots(1, 2, figsize=(16, 7), width_ratios=[0.9, 1.6])

# ---- Plot 1: GPU WGS comparison at Size=8000 ----
ax1 = axes[0]
size_max = 8000
data_p1 = gpu[gpu['Size'] == size_max]

bar_width = 0.25
x_wgs = np.arange(len(wgs_values))

for i, dev in enumerate(device_names):
    subset = data_p1[data_p1['Device'] == dev].set_index('LWS')['Time_ms']
    vals = [subset.get(w, np.nan) for w in wgs_values]
    ax1.bar(x_wgs + i * bar_width, vals, bar_width,
            label=dev, color=colors_plot1[dev])

ax1.set_xticks(x_wgs + bar_width)
ax1.set_xticklabels([str(w) for w in wgs_values])
ax1.set_yscale('log')
ax1.set_xlabel('Work Group Size')
ax1.set_ylabel('Time (ms)')
ax1.set_title(f'GPU Filter Comparison at {size_max}×{size_max}')
ax1.legend()
ax1.grid(axis='y', alpha=0.3)

# ---- Plot 2: All applicators across sizes ----
ax2 = axes[1]

best_gpu = (
    gpu[gpu['Time_ms'].notna()]
    .groupby(['Device', 'Size'])['Time_ms']
    .min()
    .reset_index()
)
best_gpu['Label'] = 'Best ' + best_gpu['Device']

cpu_plot = cpu[['Device', 'Size', 'Time_ms']].copy()
cpu_plot['Label'] = cpu_plot['Device']

combined = pd.concat([
    cpu_plot[['Label', 'Size', 'Time_ms']],
    best_gpu[['Label', 'Size', 'Time_ms']]
], ignore_index=True)

all_labels = cpu_names + ['Best Nvidia', 'Best IntelGPU', 'Best POCL']

existing_labels = [l for l in all_labels if l in combined['Label'].unique()]
label_colors = plt.cm.Set2(np.linspace(0, 1, len(existing_labels)))
color_map = dict(zip(existing_labels, label_colors))

n_sizes = len(size_values)
x_sizes = np.arange(n_sizes)
n_bars = len(existing_labels)
bar_w = 0.12

for i, lbl in enumerate(existing_labels):
    subset = combined[combined['Label'] == lbl].set_index('Size')['Time_ms']
    vals = [subset.get(s, np.nan) for s in size_values]
    offset = (i - n_bars / 2 + 0.5) * bar_w
    ax2.bar(x_sizes + offset, vals, bar_w, label=lbl, color=color_map[lbl])

ax2.set_xticks(x_sizes)
ax2.set_xticklabels([str(s) for s in size_values])
ax2.set_yscale('log')
ax2.set_xlabel('Image Size')
ax2.set_ylabel('Time (ms)')
ax2.set_title('CPU vs Best GPU Configurations Across Image Sizes')
ax2.legend(fontsize=8)
ax2.grid(axis='y', alpha=0.3)

plt.tight_layout()
plt.savefig(output_svg, format='svg')
print(f'Saved: {output_svg}')
