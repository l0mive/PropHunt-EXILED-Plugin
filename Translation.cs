namespace PropHuntMiniGame
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    public enum PluginLanguage
    {
        English,
        Russian,
        Ukrainian,
    }

    public static class Translation
    {
        private static readonly Dictionary<string, (string English, string Russian, string Ukrainian)> Texts = new()
        {
            ["permission"] = ("You need the \"PropHunt.start\" permission.", "Нужно право \"PropHunt.start\".", "Потрібен дозвіл \"PropHunt.start\"."),
            ["not_initialized"] = ("Plugin is not initialized.", "Плагин не инициализирован.", "Плагін не ініціалізований."),
            ["already_running"] = ("PropHunt is already running.", "PropHunt уже запущен.", "PropHunt вже запущено."),
            ["not_running"] = ("PropHunt is not running.", "PropHunt не запущен.", "PropHunt не запущено."),
            ["stopped"] = ("PropHunt stopped.", "PropHunt остановлен.", "PropHunt зупинено."),
            ["command_usage"] = ("Usage: prophunt [start|stop|settings [name] [value]].", "Использование: prophunt [start|stop|settings [параметр] [значение]].", "Використання: prophunt [start|stop|settings [параметр] [значення]]."),
            ["start_failed"] = ("Failed to start PropHunt. At least 2 alive non-NPC players are required. Use test_prophunt <count> for a solo NPC test.", "Не удалось запустить PropHunt. Нужно минимум 2 живых игрока, не NPC. Для одиночной проверки используйте test_prophunt <количество>.", "Не вдалося запустити PropHunt. Потрібні щонайменше 2 живі гравці, не NPC. Для одиночної перевірки використовуйте test_prophunt <кількість>."),
            ["started"] = ("PropHunt started successfully.", "PropHunt успешно запущен.", "PropHunt успішно запущено."),
            ["settings_usage"] = ("Usage: prophunt settings <name> <value>.", "Использование: prophunt settings <параметр> <значение>.", "Використання: prophunt settings <параметр> <значення>."),
            ["settings"] = ("Settings", "Настройки", "Налаштування"),
            ["settings_change"] = ("Change: prophunt settings <name> <value>.", "Изменить: prophunt settings <параметр> <значение>.", "Змінити: prophunt settings <параметр> <значення>."),
            ["invalid_setting"] = ("Unknown setting or invalid value. Use: prophunt settings.", "Неизвестный параметр или неверное значение. Используйте: prophunt settings.", "Невідомий параметр або неправильне значення. Використовуйте: prophunt settings."),
            ["setting_updated"] = ("Setting {0} updated for this plugin session.", "Параметр {0} обновлён для текущей сессии плагина.", "Параметр {0} оновлено для поточної сесії плагіна."),
            ["language_invalid"] = ("Language must be english, russian, or ukrainian.", "Язык должен быть: english, russian или ukrainian.", "Мова має бути: english, russian або ukrainian."),
            ["language_updated"] = ("Plugin language set to {0}.", "Язык плагина изменён на {0}.", "Мову плагіна змінено на {0}."),
            ["test_usage"] = ("Usage: test_prophunt <dummy_count> (1-{0}). Example: test_prophunt 6", "Использование: test_prophunt <количество_npc> (1-{0}). Пример: test_prophunt 6", "Використання: test_prophunt <кількість_npc> (1-{0}). Приклад: test_prophunt 6"),
            ["test_failed"] = ("Failed to start the solo PropHunt test. At least one alive player is required.", "Не удалось запустить одиночный тест PropHunt. Нужен хотя бы один живой игрок.", "Не вдалося запустити одиночний тест PropHunt. Потрібен хоча б один живий гравець."),
            ["test_started"] = ("Solo PropHunt test started with {0} dummies.", "Одиночный тест PropHunt запущен с {0} NPC.", "Одиночний тест PropHunt запущено з {0} NPC."),
            ["mode_test"] = ("PropHunt NPC test started", "Тест NPC PropHunt запущен", "Тест NPC PropHunt запущено"),
            ["mode_game"] = ("PropHunt started", "PropHunt запущен", "PropHunt запущено"),
            ["game_started"] = ("<color=yellow>{0}! Hunters must find real players among the bots.</color>", "<color=yellow>{0}! Охотники должны найти настоящих игроков среди NPC.</color>", "<color=yellow>{0}! Мисливці мають знайти справжніх гравців серед NPC.</color>"),
            ["hunter_hint"] = ("<color=red>You are the Hunter. Eliminate real players, not bots.</color>", "<color=red>Вы Охотник. Уничтожайте настоящих игроков, а не NPC.</color>", "<color=red>Ви Мисливець. Усуньте справжніх гравців, а не NPC.</color>"),
            ["target_hint"] = ("<color=lime>You are disguised. Blend in with the bots.</color>", "<color=lime>Вы замаскированы. Смешайтесь с NPC.</color>", "<color=lime>Ви замасковані. Змішайтеся з NPC.</color>"),
            ["time_up"] = ("Time is up! The round has ended.", "Время вышло! Раунд завершён.", "Час вийшов! Раунд завершено."),
            ["all_targets"] = ("All targets have been eliminated!", "Все цели устранены!", "Усі цілі усунено!"),
            ["wrong_target"] = ("<color=red>Wrong target! -{0} HP</color>", "<color=red>Неверная цель! -{0} HP</color>", "<color=red>Неправильна ціль! -{0} HP</color>"),
            ["wrong_target_percent"] = ("<color=red>Wrong target! -{0} HP ({1}% with {2} NPCs)</color>", "<color=red>Неверная цель! -{0} HP ({1}% при {2} NPC)</color>", "<color=red>Неправильна ціль! -{0} HP ({1}% за {2} NPC)</color>"),
            ["correct_target"] = ("<color=green>Correct target: {0}</color>", "<color=green>Верная цель: {0}</color>", "<color=green>Правильна ціль: {0}</color>"),
            ["target_eliminated"] = ("<color=orange>{0} was eliminated.</color>", "<color=orange>{0} устранён.</color>", "<color=orange>{0} усунено.</color>"),
            ["hunter_eliminated"] = ("<color=red>Your health is depleted. You are now a spectator.</color>", "<color=red>Ваше здоровье закончилось. Вы стали наблюдателем.</color>", "<color=red>Ваше здоров'я вичерпано. Ви стали спостерігачем.</color>"),
            ["hiders_win"] = ("All hunters have been eliminated. Hiders win!", "Все охотники устранены. Победа прячущихся!", "Усіх мисливців усунено. Перемога тих, хто ховається!"),
            ["test_hunters_eliminated"] = ("All hunters have been eliminated.", "Все охотники устранены.", "Усіх мисливців усунено."),
            ["start_error"] = ("PropHunt could not start: {0}", "PropHunt не удалось запустить: {0}", "Не вдалося запустити PropHunt: {0}"),
            ["npc_spawn_error"] = ("PropHunt NPC spawn attempt {0} failed: {1}", "Ошибка создания NPC PropHunt, попытка {0}: {1}", "Помилка створення NPC PropHunt, спроба {0}: {1}"),
            ["npc_create_error"] = ("PropHunt could not create an NPC after {0} attempts.", "PropHunt не удалось создать NPC за {0} попыток.", "PropHunt не вдалося створити NPC за {0} спроб."),
            ["npc_initialize_error"] = ("PropHunt could not initialize an NPC after {0} attempts.", "PropHunt не удалось инициализировать NPC за {0} попыток.", "PropHunt не вдалося ініціалізувати NPC за {0} спроб."),
            ["arena_invalid"] = ("Arena must be 049, surface_gate, or here.", "\u0410\u0440\u0435\u043d\u0430 \u0434\u043e\u043b\u0436\u043d\u0430 \u0431\u044b\u0442\u044c: 049, surface_gate \u0438\u043b\u0438 here.", "\u0410\u0440\u0435\u043d\u0430 \u043c\u0430\u0454 \u0431\u0443\u0442\u0438: 049, surface_gate \u0430\u0431\u043e here."),
            ["arena_here_requires_player"] = ("The 'here' arena can only be started by an alive player in-game.", "\u0410\u0440\u0435\u043d\u0443 'here' \u043c\u043e\u0436\u0435\u0442 \u0437\u0430\u043f\u0443\u0441\u0442\u0438\u0442\u044c \u0442\u043e\u043b\u044c\u043a\u043e \u0436\u0438\u0432\u043e\u0439 \u0438\u0433\u0440\u043e\u043a \u0432 \u0438\u0433\u0440\u0435.", "\u0410\u0440\u0435\u043d\u0443 'here' \u043c\u043e\u0436\u0435 \u0437\u0430\u043f\u0443\u0441\u0442\u0438\u0442\u0438 \u043b\u0438\u0448\u0435 \u0436\u0438\u0432\u0438\u0439 \u0433\u0440\u0430\u0432\u0435\u0446\u044c \u0432 \u0433\u0440\u0456."),
            ["arena_unavailable"] = ("The selected arena is unavailable on this map.", "\u0412\u044b\u0431\u0440\u0430\u043d\u043d\u0430\u044f \u0430\u0440\u0435\u043d\u0430 \u043d\u0435\u0434\u043e\u0441\u0442\u0443\u043f\u043d\u0430 \u043d\u0430 \u044d\u0442\u043e\u0439 \u043a\u0430\u0440\u0442\u0435.", "\u0412\u0438\u0431\u0440\u0430\u043d\u0430 \u0430\u0440\u0435\u043d\u0430 \u043d\u0435\u0434\u043e\u0441\u0442\u0443\u043f\u043d\u0430 \u043d\u0430 \u0446\u0456\u0439 \u043a\u0430\u0440\u0442\u0456."),
            ["prophunt_usage"] = ("Usage: prophunt start <049|surface_gate|here> | prophunt stop | prophunt settings [name] [value].", "\u0418\u0441\u043f\u043e\u043b\u044c\u0437\u043e\u0432\u0430\u043d\u0438\u0435: prophunt start <049|surface_gate|here> | prophunt stop | prophunt settings [\u043f\u0430\u0440\u0430\u043c\u0435\u0442\u0440] [\u0437\u043d\u0430\u0447\u0435\u043d\u0438\u0435].", "\u0412\u0438\u043a\u043e\u0440\u0438\u0441\u0442\u0430\u043d\u043d\u044f: prophunt start <049|surface_gate|here> | prophunt stop | prophunt settings [\u043f\u0430\u0440\u0430\u043c\u0435\u0442\u0440] [\u0437\u043d\u0430\u0447\u0435\u043d\u043d\u044f]."),
            ["start_usage"] = ("Usage: prophunt start <049|surface_gate|here>.", "\u0418\u0441\u043f\u043e\u043b\u044c\u0437\u043e\u0432\u0430\u043d\u0438\u0435: prophunt start <049|surface_gate|here>.", "\u0412\u0438\u043a\u043e\u0440\u0438\u0441\u0442\u0430\u043d\u043d\u044f: prophunt start <049|surface_gate|here>."),
            ["setting_saved"] = ("Setting {0} saved to the server configuration.", "\u041f\u0430\u0440\u0430\u043c\u0435\u0442\u0440 {0} \u0441\u043e\u0445\u0440\u0430\u043d\u0451\u043d \u0432 \u043a\u043e\u043d\u0444\u0438\u0433\u0443\u0440\u0430\u0446\u0438\u044e \u0441\u0435\u0440\u0432\u0435\u0440\u0430.", "\u041f\u0430\u0440\u0430\u043c\u0435\u0442\u0440 {0} \u0437\u0431\u0435\u0440\u0435\u0436\u0435\u043d\u043e \u0432 \u043a\u043e\u043d\u0444\u0456\u0433\u0443\u0440\u0430\u0446\u0456\u044e \u0441\u0435\u0440\u0432\u0435\u0440\u0430."),
            ["settings_save_failed"] = ("The setting was changed, but the server configuration could not be saved. Check the server console.", "\u041f\u0430\u0440\u0430\u043c\u0435\u0442\u0440 \u0438\u0437\u043c\u0435\u043d\u0451\u043d, \u043d\u043e \u043a\u043e\u043d\u0444\u0438\u0433\u0443\u0440\u0430\u0446\u0438\u044e \u0441\u0435\u0440\u0432\u0435\u0440\u0430 \u0441\u043e\u0445\u0440\u0430\u043d\u0438\u0442\u044c \u043d\u0435 \u0443\u0434\u0430\u043b\u043e\u0441\u044c. \u041f\u0440\u043e\u0432\u0435\u0440\u044c\u0442\u0435 \u043a\u043e\u043d\u0441\u043e\u043b\u044c \u0441\u0435\u0440\u0432\u0435\u0440\u0430.", "\u041f\u0430\u0440\u0430\u043c\u0435\u0442\u0440 \u0437\u043c\u0456\u043d\u0435\u043d\u043e, \u0430\u043b\u0435 \u043a\u043e\u043d\u0444\u0456\u0433\u0443\u0440\u0430\u0446\u0456\u044e \u0441\u0435\u0440\u0432\u0430 \u0437\u0431\u0435\u0440\u0435\u0433\u0442\u0438 \u043d\u0435 \u0432\u0434\u0430\u043b\u043e\u0441\u044f. \u041f\u0435\u0440\u0435\u0432\u0456\u0440\u0442\u0435 \u043a\u043e\u043d\u0441\u043e\u043b\u044c \u0441\u0435\u0440\u0432\u0430."),
            ["test_usage_with_arena"] = ("Usage: test_prophunt <dummy_count> <049|surface_gate|here> (1-{0}). Example: test_prophunt 6 049", "\u0418\u0441\u043f\u043e\u043b\u044c\u0437\u043e\u0432\u0430\u043d\u0438\u0435: test_prophunt <\u043a\u043e\u043b\u0438\u0447\u0435\u0441\u0442\u0432\u043e_npc> <049|surface_gate|here> (1-{0}). \u041f\u0440\u0438\u043c\u0435\u0440: test_prophunt 6 049", "\u0412\u0438\u043a\u043e\u0440\u0438\u0441\u0442\u0430\u043d\u043d\u044f: test_prophunt <\u043a\u0456\u043b\u044c\u043a\u0456\u0441\u0442\u044c_npc> <049|surface_gate|here> (1-{0}). \u041f\u0440\u0438\u043a\u043b\u0430\u0434: test_prophunt 6 049"),
        };

        public static string Get(string key, params object[] arguments)
        {
            if (!Texts.TryGetValue(key, out var values))
                return key;

            string text = GetLanguage() switch
            {
                PluginLanguage.English => values.English,
                PluginLanguage.Ukrainian => values.Ukrainian,
                _ => values.Russian,
            };

            return arguments.Length == 0 ? text : string.Format(CultureInfo.InvariantCulture, text, arguments);
        }

        public static bool TryParseLanguage(string value, out PluginLanguage language)
        {
            switch (value?.Trim().ToLowerInvariant())
            {
                case "english":
                case "en":
                    language = PluginLanguage.English;
                    return true;
                case "russian":
                case "ru":
                case "русский":
                    language = PluginLanguage.Russian;
                    return true;
                case "ukr":
                case "ukrainian":
                case "ua":
                case "uk":
                case "украинский":
                case "українська":
                    language = PluginLanguage.Ukrainian;
                    return true;
                default:
                    language = PluginLanguage.Russian;
                    return false;
            }
        }

        public static string GetLanguageName(PluginLanguage language)
        {
            return language switch
            {
                PluginLanguage.English => "English",
                PluginLanguage.Ukrainian => "Українська",
                _ => "Русский",
            };
        }

        private static PluginLanguage GetLanguage()
        {
            return Plugin.Instance?.Config?.Language ?? PluginLanguage.Russian;
        }
    }
}
