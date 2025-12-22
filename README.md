# CreditosApi

### 1.Clone e prepare o projeto:

git clone <seu-repositorio>
cd CreditosApi
dotnet restore


### 2.Suba os containers:

docker-compose up -d

### 3.Execute as migrações do banco:

dotnet ef database update --project src/CreditosApi.Infrastructure


### 4.Teste os endpoints:

#### Verificar saúde
curl http://localhost:8080/self
curl http://localhost:8080/ready

#### Inserir créditos
curl -X POST http://localhost:8080/api/creditos/integrar-credito-constituido \
  -H "Content-Type: application/json" \
  -d '[{"numeroCredito":"123456","numeroNfse":"7891011","dataConstituicao":"2024-02-25","valorIssqn":1500.75,"tipoCredito":"ISSQN","simplesNacional":true,"aliquota":5.0,"valorFaturado":30000.00,"valorDeducao":5000.00,"baseCalculo":25000.00}]'

#### Buscar créditos por NFSe
curl http://localhost:8080/api/creditos/7891011

#### Buscar crédito específico
curl http://localhost:8080/api/creditos/credito/123456

