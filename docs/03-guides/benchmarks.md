# Бенчмарки

Локальные замеры производительности (BenchmarkDotNet). В CI бенчмарки не запускаются: раннеры шумные, а прогон занимает минуты ([testing.md](testing.md)).

## Зачем

- подтвердить метрики из [vision.md](../01-product/vision.md) цифрами, а не ощущениями;
- ловить регрессии рендера и решета при изменениях;
- сравнивать «до/после» оптимизаций (например, батчинга точек).

## Проект

`benchmarks/BeautyOfNumbers.Benchmarks` — консольное приложение на BenchmarkDotNet, ссылается на `Core` и `Rendering`. Собирается вместе с решением, но не является тестовым проектом.

Классы:

| Класс | Что измеряет | Параметры |
| --- | --- | --- |
| `SieveBench` | построение решета `SievePrimeClassifier` | `1e6`, `1e7`, `1e8` |
| `SceneBuildBench` | `SceneBuilder.Build` (сцена готова, решето в `GlobalSetup`) | `100k`, `1M` |
| `DrawExportBench` | `Draw` на канвас 2000×2000 (`OutputOptions.Default`) | `100k`, `1M` |
| `DrawPreviewBench` | `SceneFitter.Fit` + `Draw` на 1280×800 | `100k` |
| `SceneBuildHeavyBench` | то же, что `SceneBuildBench`, но `10M` | `10M` |
| `DrawExportHeavyBench` | то же, что `DrawExportBench`, но `10M` | `10M` |

Кейсы 10M помечены категорией `Heavy`: сцена ≈ 240 МБ (`ScenePoint` ≈ 24 Б), поэтому они запускаются отдельно, а не вместе с быстрыми.

## Запуск

```powershell
# сначала список кейсов
dotnet run -c Release --project benchmarks/BeautyOfNumbers.Benchmarks -- --list flat

# быстрые кейсы по маске
dotnet run -c Release --project benchmarks/BeautyOfNumbers.Benchmarks -- --filter *Sieve*

# только тяжёлые кейсы 10M (долго и много памяти)
dotnet run -c Release --project benchmarks/BeautyOfNumbers.Benchmarks -- --anyCategories Heavy
```

Только `Release`: в `Debug` числа бессмысленны. Полный прогон быстрых кейсов — около 2–3 минут, тяжёлые — заметно дольше.

## Замеры

Оборудование: AMD Ryzen 7 5800H (8 физических / 16 логических ядер, 3.2 ГГц), Windows 11, .NET 8.0.31, BenchmarkDotNet 0.15.8.

### Baseline — до батчинга (2026-09-20)

Метрика «цель» — из [vision.md](../01-product/vision.md).

| Бенчмарк | Кейс | Среднее | Аллокации | Цель |
| --- | --- | --- | --- | --- |
| `SieveBench` | limit `1e6` | 0.76 мс | 61 КБ | — |
| `SieveBench` | limit `1e7` | 8.37 мс | 610 КБ | — |
| `SieveBench` | limit `1e8` | 93.4 мс | 6.1 МБ | ≤ 3 с, ≤ 256 МБ |
| `SceneBuildBench` | 100k точек | 2.43 мс | 2.29 МБ | — |
| `SceneBuildBench` | 1M точек | 24.32 мс | 22.89 МБ | — |
| `DrawExportBench` | 100k, 2000×2000 | 92.61 мс | — | — |
| `DrawExportBench` | 1M, 2000×2000 | 905.79 мс | — | — |
| `DrawPreviewBench` | 100k, 1280×800 | 91.19 мс | — | ≤ 100 мс |

Целевые метрики при этом уже выполняются: 1M = build (24 мс) + draw (906 мс) ≈ 0.93 с ≤ 2 с; 100k-превью ≈ 91 мс ≤ 100 мс. Батчинг в T3 должен дать запас по отрисовке и подтвердить метрики повторно.

### После батчинга (2026-09-20)

Тот же прогон на том же железе, после батчинга и квантования размеров в `SkiaSceneRenderer` ([rendering.md](../02-architecture/rendering.md)).

| Бенчмарк | Кейс | Baseline | После | Изменение | Цель |
| --- | --- | --- | --- | --- | --- |
| `SieveBench` | limit `1e6` | 0.76 мс | 0.81 мс | в пределах шума | — |
| `SieveBench` | limit `1e7` | 8.37 мс | 8.31 мс | в пределах шума | — |
| `SieveBench` | limit `1e8` | 93.4 мс | 105.2 мс | в пределах шума | ≤ 3 с, ≤ 256 МБ |
| `SceneBuildBench` | 100k точек | 2.43 мс | 2.42 мс | без изменений | — |
| `SceneBuildBench` | 1M точек | 24.32 мс | 23.84 мс | без изменений | — |
| `DrawExportBench` | 100k, 2000×2000 | 92.61 мс | 83.89 мс | −9 % | — |
| `DrawExportBench` | 1M, 2000×2000 | 905.79 мс | 811.36 мс | −10 % | — |
| `DrawPreviewBench` | 100k, 1280×800 | 91.19 мс | 80.37 мс | −12 % | ≤ 100 мс |

Решето в T3 не менялось; его разброс между прогонами (порядка 10 %) — шум машины.

### Тяжёлые кейсы 10M (после батчинга)

Снимались отдельно (`--anyCategories Heavy`); baseline для них не снимался — это стресс-кейс, а не продуктовый сценарий.

| Бенчмарк | Кейс | Среднее | Аллокации |
| --- | --- | --- | --- |
| `SceneBuildHeavyBench` | 10M точек | 221.1 мс | 228.9 МБ |
| `DrawExportHeavyBench` | 10M, 2000×2000 | 7.90 с | — |

10M точек — предел по памяти (сцена ≈ 240 МБ) и по терпению; практические лимиты UI/CLI ниже.

### Выводы

- Все технические метрики [vision.md](../01-product/vision.md) достигнуты: 100k-превью 80 мс ≤ 100 мс; 1M = build (24 мс) + draw (811 мс) ≈ 0.84 с ≤ 2 с; сито `1e8` 105 мс ≤ 3 с при 6.1 МБ ≤ 256 МБ.
- Батчинг ускорил отрисовку на 9–12 %; основной остаток времени — растеризация Skia, а не вызовы API.
- Раз 1M-рендер укладывается в 2 с, `--count` в CLI поднят с 100 000 до 1 000 000; UI-лимит 100k не меняется (см. [build-and-run.md](build-and-run.md)).

## Связанные документы

- [build-and-run.md](build-and-run.md)
- [testing.md](testing.md)
- [rendering.md](../02-architecture/rendering.md)
