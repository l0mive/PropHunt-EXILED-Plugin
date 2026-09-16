# PropHunt MiniGame — українська

[🇬🇧 English](README.md) · [🇷🇺 Русский](README.ru.md) · [🇺🇦 Українська](README.uk.md)

Налаштовуваний режим PropHunt для SCP: Secret Laboratory на EXILED 9.14.2+.

Один випадковий гравець стає мисливцем, інші ховаються серед NPC. Плагін підтримує плавний рух NPC, переслідування, паузи, стрибки, обхід перешкод, процентний штраф за влучання в NPC, переміщення мисливця до спостерігачів і перемогу тих, хто ховається, після усунення всіх мисливців.

## Встановлення

1. Завантажте `PropHuntMiniGame.dll` з GitHub Release.
2. Скопіюйте DLL до `EXILED/Plugins`.
3. Використовуйте EXILED 9.14.2 або суміснішу новішу версію.
4. Перезапустіть сервер.

ZIP з вихідним кодом призначений для розробників і містить нескомпільовану open-source версію.

## Права та команди

Потрібен дозвіл `PropHunt.start`. Команди працюють у Remote Admin та ігровій консолі.

| Команда | Опис |
|---|---|
| `prophunt` | Запустити звичайну гру; потрібно щонайменше 2 живі гравці, які не є NPC |
| `prophunt start` | Те саме, що `prophunt` |
| `prophunt stop` | Зупинити гру та видалити NPC |
| `prophunt settings` | Показати налаштування |
| `prophunt settings <параметр> <значення>` | Змінити налаштування на поточну сесію |
| `test_prophunt <кількість>` | Створити рівно вказану кількість NPC |

Аліаси: `startPropHunt`, `ttstart`, `testprophunt`.

```text
prophunt settings language ukrainian
prophunt settings dummies 5
prophunt settings penaltypercent 30
test_prophunt 19
```

`test_prophunt 19` працює навіть з одним живим гравцем і обмежується параметром `maxdummies`.

## Налаштування

Доступні: `language`, `dummies`, `maxdummies`, `spawnattempts`, `penalty`/`penaltypercent`, `duration`, `followchance`, `walkspeed`, `followspeed`, `turnspeed`, `pausechance`, `pausemin`, `pausemax`, `jumpchance`, `radius`, `debug`.

Мови: `english`, `russian`, `ukrainian`. Налаштування `IsEnabled`, `UniformNickname` і `UniformCustomInfo` задаються у конфігурації EXILED. Зміни через консоль діють до перезавантаження плагіна.

Кількість NPC у звичайній грі: `кількість тих, хто ховається × dummies`, але не більше `maxdummies`.

```text
фактичний штраф = penaltypercent / √(кількість живих NPC)
```

Після втрати HP мисливець стає спостерігачем. Якщо мисливців не залишилося, перемагають ті, хто ховається. Якщо всіх гравців, що ховаються, усунуто або завершився таймер, гра закінчується.

## GitHub Releases

Кожен реліз рекомендується публікувати з двома файлами:

- `PropHuntMiniGame.dll` — готовий скомпільований плагін для `EXILED/Plugins`.
- `PropHuntMiniGame-Source.zip` — нескомпільований open-source вихідний код.

До ZIP із вихідним кодом додайте `.cs`, `.csproj`, `README*` і конфігурацію проєкту. Папки `bin/` та `obj/` додавати не потрібно.

## Збірка

```powershell
dotnet restore .\PropHuntMiniGame.csproj
dotnet build .\PropHuntMiniGame.csproj
```

DLL буде створено в `bin/Debug/PropHuntMiniGame.dll`.
