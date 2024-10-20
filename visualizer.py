import matplotlib.pyplot as plt

with open('activity_report.txt', 'r') as file:
    data = file.readlines()

emails = []
commits = []

for line in data:
    parts = line.replace("Author: ", "").replace("Commits: ", "").split(", ")
    email = parts[0]
    count = int(parts[1])

    emails.append(email)
    commits.append(count)

plt.figure(figsize=(16, 10))
plt.bar(emails, commits, color='skyblue')
plt.xlabel('Authors (Emails)')
plt.ylabel('Commits')
plt.title('Commits per Author')
plt.xticks(rotation=90, fontsize=9)

plt.tight_layout()

output_path = 'commits_per_author.png'
plt.savefig(output_path, format='png', dpi=300)
