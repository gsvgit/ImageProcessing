import subprocess
import os
import datetime
from operator import itemgetter

workGroupSizes = [2**(x+3) for x in range(5)]
workPerThreads = [2**(x+1) for x in range(7)]

matrixSize = 1024
kernels = ['k2', 'k3', 'k4']
semiring = 'arithmetic'
numToRun = 10
platform = 'nvidia'
types = ['mt-byte', 'mt-int', 'mt-float32', 'mt-float64']

out_directory = 'tuning_results'

if not os.path.exists(out_directory):
    os.makedirs(out_directory)

for kernel in kernels:
    for matrixType in types: 
        res = []
        print(f'Tuning for {matrixType} and kernel {kernel} started.')
        for wgs in workGroupSizes:
            for wpt in workPerThreads:
                try: 
                    cmd = f'dotnet ./src/MatrixMultiplication/bin/Release/net9.0/MatrixMultiplication.dll --platform {platform} --kernel {kernel} --matrixsize {matrixSize} --matrixtype {matrixType} --semiring {semiring} --numtorun {numToRun} --workperthread {wpt} --workgroupsize {wgs}'
                    output = subprocess.check_output([cmd],shell=True)
                    output = output.decode("utf-8")
                    if 'Processing time:' in output:
                        time = float(output.split()[-2])
                        res.append([wgs, wpt, time])
                        print (f'wgs={wgs}, wpt={wpt}, time={time}')
                except BaseException: ()

        res = sorted(res, key=itemgetter(2))
        f = open(os.path.join(out_directory,f'{kernel}_{platform}_{matrixType}_{matrixSize}_{semiring}_{datetime.datetime.now()}.log'),'a')
        for r in res:
            print(r) 
            f.write(f'{r[0]}, {r[1]}, {r[2]}\n')
        f.close()