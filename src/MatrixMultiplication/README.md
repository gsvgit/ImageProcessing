## Matrix multiplication optimization step by step

A sequence of matrix multiplication optimizations that inspired by [OpenCL SGEMM tutorial by Cedric Nugteren](https://cnugteren.github.io/tutorial/pages/page1.html).
Our goal is to show basic optimizations, so we omit some steps represented in the tutorial.
All kernels compute **C = A * B** and are parametrized by element type and element-wise operations.

**kernel0 (K0)** is a naive kernel that accumulates results directly in **C**.

In **kernel1 (K1)** each thread uses register to accumulate **C[i,j]** and writes this value to **C** at the end of computations.
Thus we reduce global memory IO. 
This kernel reproduces [naive implementation form the tutorial](https://cnugteren.github.io/tutorial/pages/page3.html). 

**kernel2 (K2)** utilizes local memory to store tiles of matrices. The idea is based on [block matrix multiplication](https://en.wikipedia.org/wiki/Block_matrix#Multiplication). 
Respective kernel from te tutorial is a [kernel 2](https://cnugteren.github.io/tutorial/pages/page4.html).

**kernel3 (K3)** implicitly reduce data transfer between local memory and registers by computations grouping. 
Respective kernel from te tutorial is a [kernel 3](https://cnugteren.github.io/tutorial/pages/page5.html).

**kernel4 (K4)** is designed to use register aggressively to allocates tiles of matrices.
Thus we try to reduce data local memory and registers even more.
Respective kernel from te tutorial is a [kernel 6](https://cnugteren.github.io/tutorial/pages/page8.html). 
