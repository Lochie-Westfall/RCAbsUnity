import numpy as np 
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches


def processFile (url):
    data = open(url,"r")
    line = data.readline()
    values = [round(float(x)) for x in line.split()]
    return values

#ours_left_url = "/home/nubots/lochie/RCAbsUnity/ml-agents-master/UnitySDK/Assets/Resources/outcomes_ours_left.txt"
#theirs_left_url = "/home/nubots/lochie/RCAbsUnity/ml-agents-master/UnitySDK/Assets/Resources/outcomes_theirs_left.txt"
ours_left_url = "/Users/Lochie/RCAbsUnity/ml-agents-master/UnitySDK/Assets/Resources/outcomes_ours_left.txt"
theirs_left_url = "/Users/Lochie/RCAbsUnity/ml-agents-master/UnitySDK/Assets/Resources/outcomes_theirs_left.txt"

sampleSize = 1000000
showOuts = False

ours_left = processFile(ours_left_url)[-sampleSize:]
theirs_left = processFile(theirs_left_url)[-sampleSize:]

speed = 0.1
size = 5

data_points_1 = [0.5]
data_points_2 = []
data_points_3 = []
for i in range(len(ours_left)):
    temp = ours_left[i-min(i,size):i]
    if (len(temp) > 0):
        data_points_1.append((1-speed)*data_points_1[-1]+speed*(temp.count(2) / len(temp)))
        data_points_2.append(temp.count(1) / len(temp))
        data_points_3.append(temp.count(0) / len(temp))

#plt.plot(data_points_3)
#plt.plot(data_points_2)
plt.plot(data_points_1)

data_points_1 = [0.5]
data_points_2 = []
data_points_3 = []

for i in range(len(theirs_left)):
    temp = theirs_left[i-min(i,size):i]
    if (len(temp) > 0):
        data_points_1.append((1-speed)*data_points_1[-1]+speed*(temp.count(2) / len(temp)))
        data_points_2.append(temp.count(1) / len(temp))
        data_points_3.append(temp.count(0) / len(temp))

#plt.plot(data_points_3)
#plt.plot(data_points_2)
plt.plot(data_points_1)
plt.ylim(0,1)
plt.show()

