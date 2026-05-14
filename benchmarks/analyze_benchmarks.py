import pandas as pd
import matplotlib.pyplot as plt
import numpy as np
from pathlib import Path

artifacts_dir = Path(__file__).resolve().parent.parent / "BenchmarkDotNet.Artifacts" / "results"
benchmarks_dir = Path(__file__).resolve().parent

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

def load_mxm_csv(kernel_name, filename):
    path = artifacts_dir / filename
    df = pd.read_csv(path, skipinitialspace=True)
    df = df[df['Job'] == 'DefaultJob'].copy()
    df['Kernel'] = kernel_name
    df['Time_ms'] = df['Mean'].apply(parse_time_ms)
    df['N'] = df['N'].astype(int)
    df['LWS'] = df['LWS'].astype(int)
    return df

def make_config_label(row):
    k = row['Kernel']
    lws = row['LWS']
    if k in ('K0', 'K1', 'K2'):
        return f'LWS={lws}'
    elif k == 'K3':
        return f'LWS={lws} WPT={int(row["WPT"])}'
    elif k == 'K4':
        return f'LWS={lws} TTS={int(row["TTS"])}'
    return ''

def short_config_label(row):
    k = row['Kernel']
    lws = row['LWS']
    if k in ('K0', 'K1', 'K2'):
        return f'{k}\\nL{lws}'
    elif k == 'K3':
        return f'{k}\\nL{lws}W{int(row["WPT"])}'
    elif k == 'K4':
        return f'{k}\\nL{lws}T{int(row["TTS"])}'
    return ''

# ─── Image Processing Analysis ────────────────────────────────────────────

def analyze_image_processing():
    gpu_csv = artifacts_dir / "ImageProcessingBenchmarks.GpuFilterBench-report.csv"
    cpu_csv = artifacts_dir / "ImageProcessingBenchmarks.CpuFilterBench-report.csv"
    output_svg = benchmarks_dir / "benchmark_comparison.svg"

    gpu = pd.read_csv(gpu_csv, skipinitialspace=True)
    cpu = pd.read_csv(cpu_csv, skipinitialspace=True)

    gpu = gpu[gpu['Job'] == 'DefaultJob'].copy()
    gpu['Time_ms'] = gpu['Mean'].apply(parse_time_ms)
    gpu['LWS'] = gpu['LWS'].astype(int)
    gpu['Size'] = gpu['Size'].astype(int)

    cpu['Time_ms'] = cpu['Mean'].apply(parse_time_ms)
    cpu['Size'] = cpu['Size'].astype(int)

    wgs_values = sorted(gpu['LWS'].unique())
    size_values = sorted(gpu['Size'].unique())
    device_names = ['IntelGPU', 'Nvidia', 'POCL']
    cpu_names = ['CPUSequential', 'CPUParallel', 'CPUParallelRows']

    colors_plot1 = {'IntelGPU': '#4C72B0', 'Nvidia': '#DD8452', 'POCL': '#55A868'}

    fig, axes = plt.subplots(1, 2, figsize=(16, 7), width_ratios=[0.9, 1.6])

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
    label_colors_map = dict(zip(existing_labels,
                                plt.cm.Set2(np.linspace(0, 1, len(existing_labels)))))

    n_sizes = len(size_values)
    x_sizes = np.arange(n_sizes)
    n_bars = len(existing_labels)
    bar_w = 0.12

    for i, lbl in enumerate(existing_labels):
        subset = combined[combined['Label'] == lbl].set_index('Size')['Time_ms']
        vals = [subset.get(s, np.nan) for s in size_values]
        offset = (i - n_bars / 2 + 0.5) * bar_w
        ax2.bar(x_sizes + offset, vals, bar_w, label=lbl, color=label_colors_map[lbl])

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

# ─── MxM Analysis ─────────────────────────────────────────────────────────

def analyze_mxm():
    k0 = load_mxm_csv('K0', 'MatrixMultiplicationBenchmarks.K0Benchmark-report.csv')
    k1 = load_mxm_csv('K1', 'MatrixMultiplicationBenchmarks.K1Benchmark-report.csv')
    k2 = load_mxm_csv('K2', 'MatrixMultiplicationBenchmarks.K2Benchmark-report.csv')
    k3 = load_mxm_csv('K3', 'MatrixMultiplicationBenchmarks.K3Benchmark-report.csv')
    k4 = load_mxm_csv('K4', 'MatrixMultiplicationBenchmarks.K4Benchmark-report.csv')

    all_kernels = pd.concat([k0, k1, k2, k3, k4], ignore_index=True)
    all_kernels = all_kernels[all_kernels['Time_ms'].notna()].copy()
    all_kernels['ConfigLabel'] = all_kernels.apply(make_config_label, axis=1)
    all_kernels['ShortLabel'] = all_kernels.apply(short_config_label, axis=1)

    kernel_names = ['K0', 'K1', 'K2', 'K3', 'K4']
    device_names = ['IntelGPU', 'Nvidia', 'POCL']
    n_values = sorted(all_kernels['N'].unique())

    device_colors = {'IntelGPU': '#4C72B0', 'Nvidia': '#DD8452', 'POCL': '#55A868'}
    kernel_colors = {'K0': '#66c2a5', 'K1': '#fc8d62', 'K2': '#8da0cb',
                     'K3': '#e78ac3', 'K4': '#a6d854'}

    # ────────── Plot set 1: best config per kernel ──────────

    fig1, axes1 = plt.subplots(2, 3, figsize=(18, 10))
    axes1_flat = axes1.flatten()
    # hide the last (6th) subplot
    axes1_flat[5].set_visible(False)

    for idx, kernel in enumerate(kernel_names):
        ax = axes1_flat[idx]
        kdf = all_kernels[all_kernels['Kernel'] == kernel]

        best = (
            kdf.loc[kdf.groupby(['Device', 'N'])['Time_ms'].idxmin()]
            .reset_index(drop=True)
        )

        x_n = np.arange(len(n_values))
        bar_w = 0.25

        for i, dev in enumerate(device_names):
            subset = best[best['Device'] == dev].set_index('N')
            vals = [subset.at[n, 'Time_ms'] if n in subset.index else np.nan
                    for n in n_values]
            bars = ax.bar(x_n + i * bar_w, vals, bar_w,
                          label=dev, color=device_colors[dev])

            for j, (n, v) in enumerate(zip(n_values, vals)):
                if not np.isnan(v):
                    row = subset.loc[n]
                    cfg = row['ConfigLabel']
                    ax.text(x_n[j] + i * bar_w, v, cfg,
                            ha='center', va='bottom', fontsize=6, rotation=45)

        ax.set_xticks(x_n + bar_w)
        ax.set_xticklabels([str(n) for n in n_values])
        ax.set_yscale('log')
        ax.set_ylabel('Time (ms)')
        ax.set_title(f'{kernel} — Best Config per Device')
        ax.grid(axis='y', alpha=0.3)
        if idx == 0:
            ax.legend(fontsize=7)

    fig1.tight_layout()
    out1 = benchmarks_dir / "benchmark_mxm_best_cfg.svg"
    fig1.savefig(out1, format='svg')
    print(f'Saved: {out1}')

    # ────────── Plot set 2: per device, all configs ──────────

    fig2, axes2 = plt.subplots(1, 3, figsize=(32, 8), sharey=True)

    for di, dev in enumerate(device_names):
        ax = axes2[di]
        dev_df = all_kernels[all_kernels['Device'] == dev].copy()

        dev_df['SortKey'] = dev_df.apply(
            lambda r: (n_values.index(r['N']) if r['N'] in n_values else 99,
                       kernel_names.index(r['Kernel']),
                       r['LWS'],
                       r.get('WPT', 0) if 'WPT' in r else r.get('TTS', 0)),
            axis=1
        )
        dev_df = dev_df.sort_values('SortKey').reset_index(drop=True)

        bar_positions = []
        bar_labels = []
        bar_colors = []
        bar_values = []

        prev_n = None
        prev_kernel = None
        n_group_centers = []
        n_group_labels = []

        for _, row in dev_df.iterrows():
            pos = len(bar_positions)
            bar_positions.append(pos)
            bar_values.append(row['Time_ms'])
            bar_labels.append(row['ConfigLabel'])
            bar_colors.append(kernel_colors[row['Kernel']])

            if row['N'] != prev_n:
                if prev_n is not None:
                    n_group_centers.append((n_group_start + pos - 1) / 2)
                n_group_start = pos
                prev_n = row['N']

        if prev_n is not None:
            n_group_centers.append((n_group_start + len(bar_positions) - 1) / 2)

        ax.bar(bar_positions, bar_values, color=bar_colors, width=0.8)
        ax.set_xticks(bar_positions)
        ax.set_xticklabels(bar_labels, fontsize=4, rotation=90)
        ax.set_yscale('log')
        ax.set_ylabel('Time (ms)')
        ax.set_title(f'{dev} — All Configurations')
        ax.grid(axis='y', alpha=0.3)

        for nc in n_group_centers:
            ax.axvline(x=nc, color='gray', linestyle='--', linewidth=0.5, alpha=0.5)

        # add N labels using a secondary x-axis at the top
        secax = ax.secondary_xaxis(location='top')
        secax.set_xticks(n_group_centers)
        secax.set_xticklabels([f'N={n}' for n in n_values],
                              fontsize=8, fontweight='bold')
        secax.tick_params(length=0)

        from matplotlib.patches import Patch
        legend_elements = [Patch(facecolor=kernel_colors[k], label=k) for k in kernel_names]
        ax.legend(handles=legend_elements, fontsize=7)

    fig2.tight_layout()
    out2 = benchmarks_dir / "benchmark_mxm_per_device.svg"
    fig2.savefig(out2, format='svg')
    print(f'Saved: {out2}')

    # ────────── Plot set 3: HPC-style scaling overview ──────────

    fig3, axes3 = plt.subplots(2, 3, figsize=(18, 10))
    axes3_flat = axes3.flatten()
    axes3_flat[5].set_visible(False)

    line_styles = {'IntelGPU': 'o-', 'Nvidia': 's--', 'POCL': 'D-.'}

    for idx, kernel in enumerate(kernel_names):
        ax = axes3_flat[idx]
        kdf = all_kernels[all_kernels['Kernel'] == kernel]

        best = (
            kdf.loc[kdf.groupby(['Device', 'N'])['Time_ms'].idxmin()]
            .reset_index(drop=True)
        )

        title_parts = [kernel]
        for dev in device_names:
            dev_best = best[best['Device'] == dev]
            if len(dev_best):
                overall_best = dev_best.loc[dev_best['Time_ms'].idxmin()]
                title_parts.append(f'{dev}: {overall_best["ConfigLabel"]}')
            else:
                title_parts.append(f'{dev}: —')

        for dev in device_names:
            dev_best = best[best['Device'] == dev].sort_values('N')
            xs = dev_best['N'].values
            ys = dev_best['Time_ms'].values
            if len(xs) == 0:
                continue
            ax.plot(xs, ys, line_styles[dev], color=device_colors[dev],
                    label=dev, markersize=6)
            for xi, yi, cfg in zip(xs, ys, dev_best['ConfigLabel']):
                ax.annotate(cfg, (xi, yi),
                            textcoords='offset points', xytext=(0, 10),
                            ha='center', fontsize=6)

        ax.set_xlabel('Matrix Size N')
        ax.set_ylabel('Time (ms)')
        ax.set_title('  |  '.join(title_parts), fontsize=9)
        ax.set_xscale('log', base=2)
        ax.set_xticks(n_values)
        ax.get_xaxis().set_major_formatter(
            plt.FuncFormatter(lambda v, _: f'{int(v)}'))
        ax.set_yscale('log')
        ax.grid(True, alpha=0.3)
        if idx == 0:
            ax.legend(fontsize=8)

    fig3.tight_layout()
    out3 = benchmarks_dir / "benchmark_mxm_scaling.svg"
    fig3.savefig(out3, format='svg')
    print(f'Saved: {out3}')

# ─── Entry point ──────────────────────────────────────────────────────────

if __name__ == '__main__':
    import argparse
    parser = argparse.ArgumentParser(description='Analyze BenchmarkDotNet results')
    parser.add_argument('--mode', choices=['image', 'mxm'], default='image',
                        help='Which benchmarks to analyze (default: image)')
    args = parser.parse_args()

    if args.mode == 'image':
        analyze_image_processing()
    else:
        analyze_mxm()
