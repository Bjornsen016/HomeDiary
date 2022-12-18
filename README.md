# HomeDiary

Vårt projekt innehåller en Authservice, en HomeTaskService och en ExpensesService.
De är alla Microservices.

Om man vill använda HomeTaskService eller ExpensesService måste man registrera sig via AuthService och logga in. Där får man en JWT.
När en Task eller Expense läggs till så kontrolleras det först via AuthServicen ifall usern finns i databasen.

### För att starta up projektet gör så här:

1. Kör följande docker compose script: "docker-compose up"
2. Vänta tills allt är startat
3. Kör postman collection

### Endpoints

* Authservice: https://homediarygateway.azure-api.net/auth/  
* ExpensesService: https://homediarygateway.azure-api.net/expenses/
* HomeMaintenanceService: https://homediarygateway.azure-api.net/task/
