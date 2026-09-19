# Документация Beauty of Numbers

Индекс документации проекта. Читать снизу вверх или сверху вниз — по задаче.

## Как читать

| Если вы хотите | Начните с |
| --- | --- |
| Понять, что это за продукт и для кого | [01-product/vision.md](01-product/vision.md) |
| Увидеть, что уже умеет приложение | [01-product/use-cases.md](01-product/use-cases.md) |
| Знать, что будет и в каком порядке | [01-product/roadmap.md](01-product/roadmap.md) |
| Понять, как устроен код и куда двигаемся | [02-architecture/overview.md](02-architecture/overview.md) |
| Разобраться в математике спиралей | [04-math/layouts.md](04-math/layouts.md) |
| Собрать и запустить | [03-guides/build-and-run.md](03-guides/build-and-run.md) |
| Начать контрибьютить | [03-guides/contributing.md](03-guides/contributing.md) |

## Разделы

### [01-product](01-product/) — продукт

- [vision.md](01-product/vision.md) — зачем проект, для кого, какая ценность, метрики успеха.
- [use-cases.md](01-product/use-cases.md) — сценарии использования с критериями приёмки.
- [roadmap.md](01-product/roadmap.md) — вехи M0–M3 и что в них входит.
- [non-goals.md](01-product/non-goals.md) — что сознательно не делаем.
- [glossary.md](01-product/glossary.md) — словарь доменных терминов.

### [02-architecture](02-architecture/) — архитектура

- [overview.md](02-architecture/overview.md) — компоненты, зависимости, пайплайн рендера.
- [core.md](02-architecture/core.md) — доменное ядро: последовательности, классификаторы, раскладки.
- [rendering.md](02-architecture/rendering.md) — рендер на SkiaSharp, экспорт, производительность.
- [ui.md](02-architecture/ui.md) — WPF-студия: MVVM, превью, команды, хранение пресетов.
- [adr/](02-architecture/adr/) — журнал архитектурных решений.

### [03-guides](03-guides/) — руководства

- [build-and-run.md](03-guides/build-and-run.md) — сборка, запуск, тесты.
- [contributing.md](03-guides/contributing.md) — процесс работы с репозиторием.
- [testing.md](03-guides/testing.md) — стратегия тестирования.

### [04-math](04-math/) — математика

- [sequences.md](04-math/sequences.md) — числа, простые числа, сито Эратосфена.
- [layouts.md](04-math/layouts.md) — формулы и свойства спиральных раскладок.

## Правила документации

- Документация ведётся по-русски; README репозитория двуязычный.
- Идентификаторы кода, типы, имена файлов — на английском.
- Архитектурные решения оформляются как ADR в `02-architecture/adr/` и не переписываются задним числом: новое решение — новый ADR со ссылкой на старый.
- Статус документа указывается явно, если он описывает целевое состояние, а не текущее.
