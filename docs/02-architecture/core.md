# Ядро (BeautyOfNumbers.Core)

Статус: целевое состояние. Ядро — единственная часть системы, которая «знает математику», и единственная, которую можно тестировать без графики.

## Границы ответственности

Ядро умеет:

- описывать, какие числа попадают в кадр (диапазон, фильтр);
- отвечать на вопрос «простое ли число» (решето);
- раскладывать числа в точки (раскладки);
- собирать сцену — плоский массив точек;
- описывать стиль и валидировать запрос;
- сериализовать и разбирать пресеты.

Ядро не умеет и не должно: рисовать, знать про пиксели/DPI, читать файлы, зависеть от WPF или SkiaSharp, показывать диалоги.

## Модель данных

```csharp
namespace BeautyOfNumbers.Core;

public readonly record struct NumberRange(int Start, int Count)
{
    public int End => Start + Count - 1;
}

public readonly record struct Point2D(double X, double Y);

public readonly record struct RgbaColor(byte R, byte G, byte B, byte A);

public readonly record struct ScenePoint(double X, double Y, float Size, byte ColorIndex);

public sealed record Scene(
    ScenePoint[] Points,
    int WidthInPoints,      // дискретизация по X для авто-масштаба
    int HeightInPoints,
    double MinX, double MaxX,
    double MinY, double MaxY);
```

`ColorIndex` — индекс в палитре стиля (`0` — простые, `1` — составные, в будущем — другие классы). Число, а не цвет, чтобы сцена не зависела от способа отрисовки.

## Классификаторы

```csharp
public interface IPrimeClassifier
{
    int Limit { get; }          // включительно; IsPrime вне границ возвращает false
    bool IsPrime(int value);
}

public sealed class SievePrimeClassifier : IPrimeClassifier
{
    public SievePrimeClassifier(int limit);   // включительно
    public static int LimitFor(NumberRange range);  // лимит для диапазона без построения решета
    public int Limit { get; }
}
```

Требования:

- решето Эратосфена, odd-only bitset: для 10^8 — около 6,25 МБ;
- `IsPrime` — O(1) по таблице, без аллокаций;
- `limit < 2` — допустимо, все ответы `false`;
- пробное деление остаётся только как эталон для тестов (`ReferencePrimeClassifier`), в продакшене не используется.

## Раскладки

```csharp
public interface ISpiralLayout
{
    string Id { get; }                      // "archimedean", "sacks", "ulam", "hex"
    Point2D Map(int number, LayoutOptions options);
}

public sealed record LayoutOptions(
    double Rotation = 0,
    bool Clockwise = false,
    double Scale = 1.0);
```

- Раскладка — чистая функция без состояния: одинаковый вход → одинаковая точка.
- Формулы и свойства — в [../04-math/layouts.md](../04-math/layouts.md).
- Регистрация раскладок — по `Id` в реестре ядра; UI получает список, но не создаёт раскладки сам.

## Стиль и запрос

```csharp
public sealed record RenderStyle(
    RgbaColor Background,
    RgbaColor[] Palette,          // [0] простые, [1] составные
    PointSizeFunction PointSize,  // Min, Max, Curve
    bool Antialias);

public sealed record RenderRequest(
    NumberRange Range,
    bool ShowOnlyPrimes,
    string LayoutId,
    LayoutOptions Layout,
    RenderStyle Style,
    OutputOptions Output);

public sealed record OutputOptions(int Width, int Height, double Dpi);
```

- Значения по умолчанию для стиля задаются в одном месте (`RenderStyle.Default`), а не расползаются по UI.
- `ShowOnlyPrimes` — фильтр видимости: диапазон не меняется, составные просто не попадают в сцену. Это устраняет текущую неоднозначность «N чисел или N простых».
- Размер точки — функция от радиуса: константа, линейная или `√`-кривая; текущая формула `min + (max - min) * (r / rMax)^g` переносится как один из вариантов.

## Валидация

```csharp
public interface IRequestValidator
{
    IReadOnlyList<ValidationError> Validate(RenderRequest request);
}

public sealed record ValidationError(string Field, string Message);
```

- Валидация возвращает ошибки списком, а не бросает исключения: UI показывает их рядом с полями.
- Проверяется: `Count > 0`, `Start ≥ 1`, переполнение `End`, `limit` решета ≥ `End`, размеры вывода > 0, палитра не пуста.
- Исключения остаются для нарушений инвариантов в коде, а не для ввода пользователя.

## Отмена и прогресс

- Все тяжёлые операции принимают `CancellationToken` (`OperationCanceledException` — нормальный способ завершить отменённую работу).
- Этапы: `Sieve` → `Layout` → `Render` → `Encode`; прогресс — `IProgress<ProgressReport>` с этапом и долей 0..1.
- Ядро не знает про UI-поток и не вызывает `Dispatcher`: маршалинг — забота приложения.

## Пресеты

```csharp
public sealed record Preset(
    int SchemaVersion,
    string Name,
    NumberRange Range,
    bool ShowOnlyPrimes,
    string LayoutId,
    LayoutOptions Layout,
    RenderStyle Style,
    OutputOptions Output);
```

Пример JSON:

```json
{
  "schemaVersion": 1,
  "name": "Sacks 100k",
  "range": { "start": 1, "count": 100000 },
  "showOnlyPrimes": false,
  "layoutId": "sacks",
  "layout": { "rotation": 0, "clockwise": false, "scale": 1.0 },
  "style": {
    "background": "#101014",
    "palette": ["#F2C14E", "#3A3A44"],
    "pointSize": { "min": 0.5, "max": 5.0, "curve": "linear" },
    "antialias": true
  },
  "output": { "width": 3840, "height": 3840, "dpi": 300 }
}
```

Правила:

- `schemaVersion` обязателен; неизвестная мажорная версия — явная ошибка с человеческим текстом;
- сериализация — `System.Text.Json`, стабильный порядок свойств;
- круговой прогон «сохранить → загрузить» обязан давать эквивалентный объект (тест).

## Тестируемость

Что тестируется юнит-тестами:

- решето против эталонного пробного деления на отрезках 10^5–10^6;
- каждая раскладка — точечные значения `Map(1)`, `Map(2)`, `Map(10)`, периодичность/шаг;
- валидатор — таблица невалидных запросов;
- пресеты — круговой прогон, неизвестная версия, отсутствующие поля, значения по умолчанию.

## Инварианты

1. Ядро не ссылается ни на один UI/графический пакет.
2. Публичные типы неизменяемы или не имеют публичных мутаторов.
3. Все числовые границы проверяются до выделения памяти.
4. Раскладка не аллоцирует на каждый вызов, где это возможно (нет LINQ в горячем цикле).
