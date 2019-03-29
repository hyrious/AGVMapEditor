import matplotlib.pyplot as plt

years = [str(y) + '年' for y in range(2013, 2019)]
values = [2439, 3150, 4300, 6340, 12900, 18000]

bars = plt.bar(years, values)
for i, rect in enumerate(bars):
  height = rect.get_height()
  plt.text(rect.get_x() + rect.get_width()/2.0, height, str(values[i]), ha='center', va='bottom')

plt.rcParams['font.sans-serif'] = ['SimHei']
plt.savefig("bar.pdf")
