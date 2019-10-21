import matplotlib.pyplot as plt
import numpy as np
import sys

assert (len(sys.argv)>3), "usage: python this_url data_url detail sample_size" 

rows = 12 + 2
columns = 18 + 2
detail = int(sys.argv[2])
sample_size = round(int(sys.argv[3]))
heat = np.zeros((rows*detail, columns*detail))

url = sys.argv[1]
data = open(url,"r")

page_length = sum(1 for l in data)
data = open(url,"r")
line = data.readline()
line_num = page_length
while line:
	if page_length < sample_size or line_num < sample_size:
		value = line.split()
		heat[-round((float(value[0])+rows/2)*detail),-round((float(value[1])+columns/2)*detail)] += 1
	line_num -= 1
	line = data.readline()

data.close()

plt.imshow(heat, cmap='hot', interpolation='nearest')
plt.show()
