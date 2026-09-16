# PropHunt MiniGame — русский

[🇬🇧 English](README.md) · [🇷🇺 Русский](README.ru.md) · [🇺🇦 Українська](README.uk.md)

Настраиваемый режим PropHunt для SCP: Secret Laboratory на EXILED 9.14.2+.

Один случайный игрок становится охотником, остальные прячутся среди NPC. Поддерживаются плавное движение NPC, следование, паузы, прыжки, обход препятствий, процентный штраф за стрельбу по NPC, перевод охотника в наблюдатели и победа прячущихся после устранения всех охотников.

## Установка

1. Скачайте `PropHuntMiniGame.dll` из GitHub Release.
2. Скопируйте DLL в `EXILED/Plugins`.
3. Используйте EXILED 9.14.2 или совместимую более новую версию.
4. Перезапустите сервер.

Исходный ZIP предназначен для разработчиков и содержит несобранную open-source версию.

## Права и команды

Требуется право `PropHunt.start`. Команды работают в Remote Admin и игровой консоли.

| Команда | Описание |
|---|---|
| `prophunt` | Запустить обычную игру; нужно минимум 2 живых игрока, не NPC |
| `prophunt start` | То же, что `prophunt` |
| `prophunt stop` | Остановить игру и удалить NPC |
| `prophunt settings` | Показать настройки |
| `prophunt settings <параметр> <значение>` | Изменить настройку на текущую сессию |
| `test_prophunt <количество>` | Создать ровно указанное количество NPC |

Алиасы: `startPropHunt`, `ttstart`, `testprophunt`.

```text
prophunt settings language russian
prophunt settings dummies 5
prophunt settings penaltypercent 30
test_prophunt 19
```

`test_prophunt 19` работает даже с одним живым игроком и ограничивается параметром `maxdummies`.

## Настройки

Доступны: `language`, `dummies`, `maxdummies`, `spawnattempts`, `penalty`/`penaltypercent`, `duration`, `followchance`, `walkspeed`, `followspeed`, `turnspeed`, `pausechance`, `pausemin`, `pausemax`, `jumpchance`, `radius`, `debug`.

Языки: `english`, `russian`, `ukrainian`. Настройки `IsEnabled`, `UniformNickname` и `UniformCustomInfo` задаются в EXILED-конфигурации. Изменения через консоль действуют до перезагрузки плагина.

Количество NPC в обычной игре: `количество прячущихся × dummies`, но не больше `maxdummies`.

```text
фактический штраф = penaltypercent / √(количество живых NPC)
```

При потере HP охотник становится наблюдателем. Если охотников не осталось, побеждают прячущиеся. Если устранены все прячущиеся или закончился таймер, игра завершается.

## GitHub Releases

Каждый релиз рекомендуется публиковать с двумя файлами:

- `PropHuntMiniGame.dll` — готовый собранный плагин для `EXILED/Plugins`.
- `PropHuntMiniGame-Source.zip` — несобранный open-source исходный код.

В исходный ZIP включите `.cs`, `.csproj`, `README*` и конфигурацию проекта. Папки `bin/` и `obj/` добавлять не нужно.

## Сборка

```powershell
dotnet restore .\PropHuntMiniGame.csproj
dotnet build .\PropHuntMiniGame.csproj
```

DLL появится в `bin/Debug/PropHuntMiniGame.dll`.
