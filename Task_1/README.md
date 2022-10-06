### **Для запуска программы:**
1. Перейти в директорию Task_1/ModelLib:
```
cd Task_1/ModelLib
```

2. Сформировать пакет NuGet:
```
dotnet pack
```

3. Перейти в директорию ConsoleApp:
```
cd ../ConsoleApp
```

4. Запуск приложения:
```
dotnet run [files] [options]
```
options:
```
--сancellation
```
Возможные значения: true/false.
Default: false.

```
--token
```
Возможные значения: shared/individual. Где "shared" - общий токен, "individual" - отдельный для каждой задачи.
Default: shared.

Для обработки всех изображений из пула запустить команду:
```
dotnet run all
```
5. Примеры команд:
```
dotnet run all
dotnet run anger.jpg
dotnet run fear.jpg ImagesSource/happiness.jpg 
dotnet run all --cancellation=true --token=individual
dotnet run fear.jpg contempt.jpg happiness.jpg --cancellation=true
```