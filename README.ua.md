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

## Інформація про плагін

- **Гра:** SCP: Secret Laboratory
- **Версія плагіна:** 0.2.1
- **EXILED:** 9.14.2+
- **Вихідний код:** [github.com/l0mive/PropHunt-EXILED-Plugin](https://github.com/l0mive/PropHunt-EXILED-Plugin)
- **Остання версія:** [GitHub Releases](https://github.com/l0mive/PropHunt-EXILED-Plugin/releases/latest)
- **Залежності:** EXILED 9.14.2+ та .NET Framework 4.8

Власник сервера самостійно відповідає за відповідність використання плагіна [Community Server Guidelines](https://scpslgame.com/CSG.pdf).

## Права та команди

Потрібен дозвіл `PropHunt.start`. Команди працюють у Remote Admin та ігровій консолі.

| Команда | Опис |
|---|---|
| `prophunt` | Показати правильне використання команди |
| `prophunt start <локація>` | Запустити звичайну гру на `049`, `surface_gate` або `here`; потрібно щонайменше 2 живі гравці, які не є NPC |
| `prophunt stop` | Зупинити гру та видалити NPC |
| `prophunt settings` | Показати налаштування |
| `prophunt settings <параметр> <значення>` | Змінити налаштування та зберегти його до конфігурації сервера |
| `test_prophunt <кількість> <локація>` | Створити рівно вказану кількість NPC на `049`, `surface_gate` або `here` |

Аліаси: `startPropHunt`, `ttstart`, `testprophunt`.

```text
prophunt start 049
prophunt settings language ukrainian
prophunt settings dummies 5
prophunt settings penaltypercent 30
test_prophunt 19 surface_gate
```

`test_prophunt 19 surface_gate` працює навіть з одним живим гравцем і обмежується параметром `maxdummies`.

## Налаштування

Доступні: `language`, `dummies`, `maxdummies`, `spawnattempts`, `penalty`/`penaltypercent`, `duration`, `followchance`, `walkspeed`, `followspeed`, `turnspeed`, `pausechance`, `pausemin`, `pausemax`, `jumpchance`, `radius`, `debug`.

Локація обирається лише під час запуску: `prophunt start <локація>` або `test_prophunt <кількість> <локація>`. Доступні `049`, `surface_gate` і `here`. Пресет `049` використовує перевірену точку підлоги біля дверей SCP-049 Armory, подалі від колайдера воріт; перед телепортацією для кожної позиції гравця та NPC перевіряються вільний простір і потрібний рівень підлоги. Двері й ворота секції відчиняються та блокуються, а ліфт SCP-049 лишається зачиненим і заблокованим. `surface_gate` починає гру з боку поверхні біля Surface Gate: зовнішні двері відчиняються та блокуються, ліфти Gate A і Gate B блокуються. `here` використовує місце живого гравця, який запустив гру, тому цю локацію не можна запустити із серверної консолі.

Мови: `english`, `russian`, `ukrainian`. Налаштування `IsEnabled`, `UniformNickname` і `UniformCustomInfo` задаються у конфігурації EXILED. Кожна успішна зміна через `prophunt settings` одразу зберігається до конфігурації сервера та лишається після перезапуску.

Кількість NPC у звичайній грі: `кількість тих, хто ховається × dummies`, але не більше `maxdummies`.

```text
фактичний штраф = penaltypercent / √(кількість живих NPC)
```

Після втрати HP мисливець стає спостерігачем. Якщо мисливців не залишилося, перемагають ті, хто ховається. Якщо всіх гравців, що ховаються, усунуто або завершився таймер, гра закінчується.

## Завантаження

Завантажте **`PropHuntMiniGame.dll`** з останнього релізу та скопіюйте його до папки сервера `EXILED/Plugins`.

Файл **`PropHuntMiniGame-Source.zip`** містить нескомпільований open-source проєкт для розробників, які хочуть переглянути або зібрати плагін.

## Збірка

```powershell
dotnet restore .\PropHuntMiniGame.csproj
dotnet build .\PropHuntMiniGame.csproj
```

DLL буде створено в `bin/Debug/PropHuntMiniGame.dll`.
