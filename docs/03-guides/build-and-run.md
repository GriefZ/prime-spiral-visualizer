# Сборка и запуск

## Требования

| Что | Версия |
| --- | --- |
| .NET SDK | 8.0 или новее (проекты нацелены на `net8.0`) |
| ОС | Windows 10/11 — для студии (WPF); CLI и библиотеки кроссплатформенны |
| IDE | Visual Studio 2022, Rider или VS Code с C# Dev Kit |

Проверка SDK:

```powershell
dotnet --list-sdks
```

## Сборка

Текущее решение:

```powershell
dotnet build PrimeSpiralVisualizer.sln
```

После вехи M0 решение переименовывается в `BeautyOfNumbers.sln` ([ADR-0001](../02-architecture/adr/0001-core-split-and-rename.md)):

```powershell
dotnet build BeautyOfNumbers.sln
```

## Запуск

### Студия (WPF)

```powershell
dotnet run --project PrimeSpiralVisualizerUI
```

После M0:

```powershell
dotnet run --project src/BeautyOfNumbers.App.Wpf
```

Что делать в приложении: задать диапазон чисел, выбрать раскладку и цвета, нажать «Generate Image» (в M1 превью обновляется само), затем «Save Image».

### Консольный пример

```powershell
dotnet run --project PrimeSpiralVisualizer
```

Генерирует `plot80000TrColored9.png` в рабочей папке (каталог проекта при запуске через `dotnet run`).

После M0:

```powershell
dotnet run --project src/BeautyOfNumbers.Cli -- render --preset examples/sacks-100k.json --output out.png
```

> Формат аргументов CLI в M1–M3 уточняется; следите за [roadmap.md](../01-product/roadmap.md).

## Тесты

```powershell
dotnet test
```

Подробнее — [testing.md](testing.md).

## Типичные проблемы

| Симптом | Решение |
| --- | --- |
| `NETSDK1045` / «SDK не поддерживает .NET 8» | Установить .NET 8 SDK; более новые SDK (9/10) тоже подходят. |
| Проект `App.Wpf` не собирается вне Windows | Это ожидаемо: WPF только под Windows. Собирайте остальные проекты или используйте кроссплатформенный CI. |
| Долгий первый `dotnet build` | Восстановление пакетов; повторные сборки быстрее. |
| PNG не появляется | Проверьте рабочую папку процесса (`dotnet run` использует каталог проекта); путь выводится в консоль. |
| Ошибки доступа к файлу при экспорте | Файл открыт в другой программе; UI должен сообщить об этом явно (в прототипе ошибка проглатывается — чинится в M1). |

## Связанные документы

- [contributing.md](contributing.md)
- [testing.md](testing.md)
