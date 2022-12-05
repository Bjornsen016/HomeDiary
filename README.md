# HomeDiary

För att starta up projektet gör så här:

1. Bygg imagen för AuthService genom att ställa dig i AuthService mappen och köra följande script: "docker build -t authservice:latest ."
2. Skriv cd .. i cmd
3. Kör följande docker compose script: "docker-compose -f deploy.yml up"
4. Vänta tills allt är startat
5. Kör postman collection
