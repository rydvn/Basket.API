## Basket API

### İçerik

-   IdentityServer4
-   MongoDb Data Context
-   Redis Cache Data Context
-   Redlock .NET *(Redis Cache tabanlı Distributed Lock)*
-   RabbitMq *(event oluşturma)*
-   HttpClientFactory Data Context *(Encapsulated Typed Client)* **(her bir dış API için ayrı bir tane DataContext oluşturulmalıdır)**
-   OpenAPI v3 *(swagger’ın yenisi, IAM ile auth destekli)*
-   Jaeger
-   Graylog

### Kullanım

Repo’yu fork’ladık sonra ilk iş olarak “**Basket API**” adını

-   Dosyaların içinde (solution, project, dockerfile, kod, infra\helmcharts)
-   Folder adlarında
-   Project Namespace’inde (**Basket.API** olarak geçiyor boşluk kabul etmediği için)

yeni repo adına uygun olarak rename etmeniz gerekmektedir.

Sonrasında ise kullanmadığınız kısımlar hem startup.cs, hem appsettings.json hem de katmanlardan (Controller, Service, Data, Model) temizlemek olmalıdır.

Swagger tarafı için ise:

-   Constants altındaki “**ApiConfigurationConsts**” içindeki “**ApiName**” değerini
-   “Startup.cs” dosyasındaki ve “infra\helmcharts” altındaki “values.yaml”daki basePath/ingress.path

değiştirmeniz gerekmektedir.

IAM tarafı için “**OidcSwaggerUIClientId**” ve “**OidcApiName**”

-   Constants altındaki “AuthorizationConsts.cs”
-   Appsettings.json’daki “IdentityServerApiConfiguration” configuration section’ında

değiştirmeniz gerekmektedir.

### Notlar

-	**appsettings.json** içindeki konfigürasyonlar generic olacak şekilde verilmiştir, dolayısıyla kullanmaya başlamadan önce kendi repo’nuzda doldurmanız gerekmektedir.
-	IAM tarafında Client tanımlanması yapılması gerekmektedir.
