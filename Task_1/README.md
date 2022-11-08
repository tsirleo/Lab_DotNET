# **Task_1**
Библиотека, в которой создается **NuGet-пакет**, использующий модель машинного обучения для определения вероятности эмоций на изображении ([EmotionFerPlus model](https://github.com/onnx/models/tree/main/vision/body_analysis/emotion_ferplus)), и **консольное приложение С#** для его тестирования и демонстрации.

## **Для запуска программы:** 
**1. Перейти в директорию Task_1/ModelLib:**
```
cd Task_1/ModelLib
```

**2. Сформировать пакет NuGet:**
```
dotnet pack
```

**3. Перейти в директорию ConsoleApp:**
```
cd ../ConsoleApp
```

**4. Запуск приложения:**
```
dotnet run [files] [options]
```
#### options:
```
--сancellation
```
- *Возможные значения:* ***true/false***.
- *Default:* ***false***.
```
--token
```
- *Возможные значения:* ***shared/individual***. Где *"shared"* - общий токен, *"individual"* - отдельный для каждой задачи. 
- *Default:* ***shared***.

***Для обработки всех изображений из пула запустить команду:***
```
dotnet run all
```
**5. Примеры команд:**
```
dotnet run all
dotnet run anger.jpg
dotnet run fear.jpg ImagesSource/happiness.jpg 
dotnet run all --cancellation=true --token=individual
dotnet run fear.jpg contempt.jpg happiness.jpg --cancellation=true
```
## **Условие:** 
Требуется разработать компонент для анализа изображений с применением готовой нейронной сети в формате ONNX. Сеть **EmotionFerPlus** для определения эмоций. 

#### **Требования к реализации:** 
- Компонент для распознавания должен принимать в качестве входных данных изображение в формате JPG или PNG и выдавать результат анализа изображения.  
- Компонент размещен в отдельном пакете NuGet и должен быть переиспользован в последующих заданиях без перекомпиляции. Компонент должен быть максимально независим от среды использования, в частности: 
  1. не использовать консольный ввод/вывод  
  2. принимать содержимое файла в виде массива или потока байт. 
    >Документация по созданию пакетов NuGet (Visual Studio Code): https://learn.microsoft.com/en-us/nuget/create-packages/creating-a-package-dotnet-cli 
- Для распознавания изображений применяются заранее обученные модели из [репозитория](https://github.com/onnx/models/tree/main/vision/body_analysis/emotion_ferplus/model). Файл с моделью должен содержаться в сборке как встроенный ресурс.
    >Как включить файл контента в пакет NuGet:
    https://devblogs.microsoft.com/nuget/nuget-contentfiles-demystified/ 
- В целях экономии памяти компонент должен создавать только один экземпляр модели (сессии), но предоставлять асинхронный потокобезопасный API с возможностью отмены вычисления при помощи CancellationToken. 
- Пакет Nuget должен использоваться в консольном приложении. Приложение должно выдавать вероятности всех классов эмоций для изображения.  