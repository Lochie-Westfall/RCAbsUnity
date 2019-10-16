import matplotlib.pyplot as plt
import numpy as np

rows = 12
columns = 18
detail = 10
heat = np.zeros((rows*detail, columns*detail))

data = open("position_data.txt","r")

line = data.readline()

while line:
    value = line.split()
    heat[round((float(value[0])+rows/2)*detail),round((float(value[1])+columns/2)*detail)] += 1
    line = data.readline()

data.close()

plt.imshow(heat, cmap='hot', interpolation='nearest')
plt.show()
