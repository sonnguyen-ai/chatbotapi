**How to run the local:**

1- Open vscode f5

![image](https://github.com/user-attachments/assets/ccfd9b56-37c1-4004-b97b-14f1317ab11c)

2- [http://localhost:5194/swagger](http://localhost:5194/swagger/index.html) (admin/123456)

3- cosmos db only for prod, inmem db for dev env.

{
  "repositoryType": "CosmosSettingsRepository",
  "environment": "Production"
} or

{
  "repositoryType": "InMemorySettingsRepository",
  "environment": "Development"
}


