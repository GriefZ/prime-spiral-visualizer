# UI: студия (BeautyOfNumbers.App.Wpf)

Статус: целевое состояние. Заменяет `PrimeSpiralVisualizerUI`.

## Границы ответственности

Приложение знает про WPF, Skia-превью и диалоги. Оно не содержит математики: всё, что считает, живёт в `Core` и `Rendering`.

## Структура проекта

```text
BeautyOfNumbers.App.Wpf/
  App.xaml(.cs)               — ресурсы, конвертеры, DI-корни (без магии)
  Views/
    MainWindow.xaml(.cs)      — окно: панель параметров + превью
  ViewModels/
    MainWindowViewModel.cs    — состояние, команды, валидация
    PresetListItemViewModel.cs
  Controls/
    SpiralPreviewControl.cs   — обёртка над SKElement: ввод, Viewport, отрисовка сцены
  Services/
    RenderScheduler.cs        — debounce, отмена, запуск сцены в фоне
    PresetStore.cs            — чтение/запись пресетов в %AppData%
    DialogService.cs          — файловые диалоги, сообщения, подтверждения
    AppLogger.cs              — файловый лог
  Resources/
    Styles.xaml, Converters/  — стили и конвертеры (BooleanToVisibility и т.п.)
```

## Экран

- Слева — панель параметров: диапазон (`Start`, `Count`), «только простые», раскладка и её параметры, цвета (фон, простые, составные), размер точек (min/max/кривая), размер экспорта.
- По центру — превью на `SpiralPreviewControl`.
- Снизу — статус: реальный прогресс с этапом, кнопка «Отмена».
- Меню: «Пресеты» (сохранить/загрузить/недавние), «Экспорт PNG», «Сбросить вид», «О программе».

## Превью

`SpiralPreviewControl` — тонкая обёртка:

- владеет `SKElement` и текущими `Scene`/`RenderStyle`/`Viewport`;
- ЛКМ-драг — панорама, колесо — зум к позиции курсора, двойной щелчок — сброс вида;
- `PaintSurface` только вызывает `ISceneRenderer.Draw` — никакой математики и аллокаций в UI-потоке;
- вид (`Viewport`) — состояние представления, а не модели: в JSON-пресет он не попадает;
- публикует события для статусной строки (координаты/масштаб — по желанию).

## ViewModel

Состояние:

| Свойство | Смысл |
| --- | --- |
| `NumberRangeStart`, `NumberRangeCount` | Диапазон чисел. |
| `ShowOnlyPrimes` | Фильтр видимости (не меняет диапазон). |
| `SelectedLayoutId`, `LayoutOptions` | Раскладка. |
| `BackgroundColor`, `PrimeColor`, `CompositeColor`, `PointSizeMin/Max/Curve` | Стиль. |
| `ExportWidth`, `ExportHeight`, `ExportDpi` | Вывод. |
| `Scene`, `IsBusy`, `Progress`, `ProgressStage`, `ValidationErrors` | Текущее состояние. |

Команды: `GenerateCommand` (если live-режим выключен), `CancelCommand`, `SavePngCommand`, `SavePresetCommand`, `LoadPresetCommand`, `ResetViewCommand`, `ZoomInCommand`, `ZoomOutCommand`.

Правила:

- любое изменение параметра планирует перерисовку через `RenderScheduler` (debounce 150–250 мс);
- новый запрос отменяет предыдущий незавершённый;
- `SavePngCommand` использует уже построенную сцену и рендерит её в полном разрешении; если параметры изменились — перестраивает;
- команды недоступны во время занятости там, где это имеет смысл; `CancelCommand` — доступна;
- валидация — из ядра (`IRequestValidator`), ошибки показываются рядом с полями, а не модальными окнами.

## Семантика параметров

Текущая неоднозначность «Number Count — это числа или простые?» устраняется:

- диапазон — всегда целые числа `[Start, Start + Count)`;
- простые — это фильтр отображения `ShowOnlyPrimes`, а не другой способ генерации;
- в CLI те же понятия и те же имена.

## Пресеты

- Хранилище: `%AppData%\BeautyOfNumbers\presets\<name>.json`.
- Список пресетов в меню; сохранение с запросом имени; загрузка — установка параметров + перерисовка.
- Битый или несовместимый пресет не роняет приложение: сообщение, пресет пропускается.
- Последние использованные настройки восстанавливаются при старте (`settings.json` рядом с пресетами).

## Ошибки и логи

- Ошибки ввода — инлайн, без диалогов.
- Ошибки ввода-вывода — диалог с человеческим текстом и запись в лог.
- Необработанные исключения UI-потока логируются и показываются как «что-то пошло не так»; приложение не должно молча падать.
- Логи: `%AppData%\BeautyOfNumbers\logs\app.log`, ротация по размеру.

## Что удаляется из текущего кода

| Сейчас | Почему |
| --- | --- |
| `MainWindowViewModel.GeneratePrimes` | Дубликат; простые считает ядро. |
| `MainWindowViewModel.GenerateSequentialNumbers` | То же; диапазон — параметр запроса. |
| `GenerateAndDisplayImage` с `temp_preview.png` | Заменяется live-превью ([ADR-0003](adr/0003-live-preview-no-temp-files.md)). |
| Фейковый `Progress = 10/50/80/90` | Заменяется `ProgressReport` из рендера. |
| `LoadPreviewImage` через `BitmapImage` из файла | Больше не нужен. |
| Зум через `ScaleTransform` поверх `Image` | Заменяется `Viewport` в Skia-превью. |
| Хардкод лимитов 10..100 000 | Лимиты выводятся из решета и выбранного диапазона. |

`RelayCommand` и конвертеры переезжают как есть; вопрос замены на `CommunityToolkit.Mvvm` — открыт до M0 (см. [overview.md](overview.md#открытые-вопросы)).

## Открытые вопросы

| Вопрос | Когда решаем |
| --- | --- |
| MVVM-тулкит и объём шаблонного кода | M0 |
| Нужны ли док-панели/темы интерфейса | M2 |
| Хранение `Viewport` в пресете (опционально) | M2 |
