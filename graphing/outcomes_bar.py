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

sampleSize = 10000
showOuts = False

ours_left = processFile(ours_left_url)[-sampleSize:]
theirs_left = processFile(theirs_left_url)[-sampleSize:]


conditions = ['Learning Team Kickoff', 'Clumping Team Kickoff', 'Both']
wins = np.array([ours_left.count(2), theirs_left.count(2), ours_left.count(2) + theirs_left.count(2)])
outs = np.array([0,0,0])
if showOuts:
    outs = np.array([ours_left.count(1), theirs_left.count(1), ours_left.count(1) + theirs_left.count(1)])
losses = np.array([ours_left.count(0), theirs_left.count(0), ours_left.count(0) + theirs_left.count(0)])
ind = [x for x, _ in enumerate(conditions)]

total = wins + outs + losses
proportion_wins = np.true_divide(wins, total) * 100
proportion_outs = np.true_divide(outs, total) * 100
proportion_losses = np.true_divide(losses, total) * 100

plt.bar(ind, proportion_wins, width=0.8, label='wins', color='green', bottom=proportion_losses+proportion_outs)
plt.bar(ind, proportion_outs, width=0.8, label='outs', color='grey', bottom=proportion_losses)
plt.bar(ind, proportion_losses, width=0.8, label='losses', color='red')

green_patch = mpatches.Patch(color='green', label='Learning team goals')
grey_patch = mpatches.Patch(color='grey', label="Ball out")
red_patch = mpatches.Patch(color='red', label='Clumping team goals')
plt.legend(handles=[green_patch, grey_patch, red_patch])

plt.xticks(ind, conditions)
plt.ylabel("outcome percentage")
#plt.xlabel("conditions")
plt.title("RCAbsUnity Round Outcomes")
plt.ylim=1.0

plt.setp(plt.gca().get_xticklabels(), rotation=0, horizontalalignment='center')

plt.show()