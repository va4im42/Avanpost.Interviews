# Решение тестового
* Развернута бд Postgres в Docker;
* Заполнена тестовыми данными с помощью утилиты Avanpost.Interviews.Task.Integration.Data.DbCreationUtility;
* Реализован интерфейс коннектора:
```csharp
        public ILogger Logger { get; set; } //Логгер, нужен для логирования в тестах
        void StartUp(string connectionString); //Конфигурация коннектора через строку подключения (настройки для подключения к ресурсу(строка подключения к бд, 
        // путь к хосту с логином и паролем, дополнительные параметры конфигурации бизнес-логики и тд, формат любой, например: "key1=value1;key2=value2...";
        void CreateUser(UserToCreate user);// Создать пользователя со свойствами
        IEnumerable<Property> GetAllProperties();// Получить все свойства, которые можно получить для пользователя, пароль тоже свойство
        IEnumerable<UserProperty> GetUserProperties(string userLogin);// Получить все значения свойств пользователя
        bool IsUserExists(string userLogin);// Проверка существования пользователя
        void UpdateUserProperties(IEnumerable<UserProperty> properties, string userLogin);// Изменения значений свойств пользователя
        IEnumerable<Permission> GetAllPermissions();// Получить все права в системе
        void AddUserPermissions(string userLogin, IEnumerable<string> rightIds);// Добавить права пользователю в системе
        void RemoveUserPermissions(string userLogin, IEnumerable<string> rightIds);// Удалить права пользователю в системе
        IEnumerable<string> GetUserPermissions(string userLogin);// Получить права пользователя в системе
```

# Структура решения:
* Avanpost.Interviews.Task.Integration.SandBox.Tests - проект с тестами коннектора;
* Avanpost.Interviews.Task.Integration.SandBox.Connector - проект с реализуемым коннектором

# Реализации интерфейса коннектора
* Коннектор реализует интерфейс IConnector (все методы интерфейса);
* Коннектор проходит все тесты (так как реаливано реализовано подключение только для Postgres, проходит 9/18 тестов);
* Коннектор не изменяет данные в таблицах RequestRights и ItRole;
* Коннектор использует логирование через свойство Logger;
* При работе с Permission разделяет ItRole и RequestRight;


