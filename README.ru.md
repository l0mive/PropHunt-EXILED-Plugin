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

## Информация о плагине

- **Игра:** SCP: Secret Laboratory
- **Версия плагина:** 0.2.1
- **EXILED:** 9.14.2+
- **Исходный код:** [github.com/l0mive/PropHunt-EXILED-Plugin](https://github.com/l0mive/PropHunt-EXILED-Plugin)
- **Последняя версия:** [GitHub Releases](https://github.com/l0mive/PropHunt-EXILED-Plugin/releases/latest)
- **Зависимости:** EXILED 9.14.2+ и .NET Framework 4.8

Владелец сервера самостоятельно отвечает за соответствие использования плагина [правилам Community Server Guidelines](https://scpslgame.com/CSG.pdf).

## Права и команды

Требуется право `PropHunt.start`. Команды работают в Remote Admin и игровой консоли.

| Команда | Описание |
|---|---|
| `prophunt` | Показать правильное использование команды |
| `prophunt start <локация>` | Запустить обычную игру на `049`, `surface_gate` или `here`; нужно минимум 2 живых игрока, не NPC |
| `prophunt stop` | Остановить игру и удалить NPC |
| `prophunt settings` | Показать настройки |
| `prophunt settings <параметр> <значение>` | Изменить настройку и сохранить её в конфигурацию сервера |
| `test_prophunt <количество> <локация>` | Создать ровно указанное количество NPC на `049`, `surface_gate` или `here` |

Алиасы: `startPropHunt`, `ttstart`, `testprophunt`.

```text
prophunt start 049
prophunt settings language russian
prophunt settings dummies 5
prophunt settings penaltypercent 30
test_prophunt 19 surface_gate
```

`test_prophunt 19 surface_gate` работает даже с одним живым игроком и ограничивается параметром `maxdummies`.

## Настройки

Доступны: `language`, `dummies`, `maxdummies`, `spawnattempts`, `penalty`/`penaltypercent`, `duration`, `followchance`, `walkspeed`, `followspeed`, `turnspeed`, `pausechance`, `pausemin`, `pausemax`, `jumpchance`, `radius`, `debug`.

Локация выбирается только при запуске: `prophunt start <локация>` или `test_prophunt <количество> <локация>`. Доступны `049`, `surface_gate` и `here`. Пресет `049` использует проверенную точку пола рядом с дверью SCP-049 Armory, вдали от коллайдера ворот; перед телепортом для каждой позиции игрока и NPC проверяются свободное место и нужный уровень пола. Двери и ворота секции открываются и блокируются, а лифт SCP-049 остаётся закрытым и заблокированным. `surface_gate` начинает игру со стороны поверхности у Surface Gate: наружные двери открываются и блокируются, лифты Gate A и Gate B блокируются. `here` использует место живого игрока, запустившего игру, поэтому эту локацию нельзя запустить из серверной консоли.

Языки: `english`, `russian`, `ukrainian`. Настройки `IsEnabled`, `UniformNickname` и `UniformCustomInfo` задаются в EXILED-конфигурации. Каждое успешное изменение через `prophunt settings` сразу сохраняется в конфигурацию сервера и остаётся после перезапуска.

Количество NPC в обычной игре: `количество прячущихся × dummies`, но не больше `maxdummies`.

```text
фактический штраф = penaltypercent / √(количество живых NPC)
```

При потере HP охотник становится наблюдателем. Если охотников не осталось, побеждают прячущиеся. Если устранены все прячущиеся или закончился таймер, игра завершается.

## Скачать

Скачайте **`PropHuntMiniGame.dll`** из последнего релиза и скопируйте его в папку сервера `EXILED/Plugins`.

Файл **`PropHuntMiniGame-Source.zip`** содержит несобранный open-source проект для разработчиков, которые хотят изучить или собрать плагин.

## Сборка

```powershell
dotnet restore .\PropHuntMiniGame.csproj
dotnet build .\PropHuntMiniGame.csproj
```

DLL появится в `bin/Debug/PropHuntMiniGame.dll`.
