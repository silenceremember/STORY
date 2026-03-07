#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Story.Data;

namespace Story.Editor
{
    /// <summary>
    /// Tools → Story → Generate Event Assets
    /// ~20 событий для 10-дневной кампании с цепочками (flags).
    /// Фраза на кнопке: "{actionVerb} {approach} {support}"
    /// </summary>
    public static class EventAssetGenerator
    {
        private struct EventData
        {
            public string key;
            public string eventText;
            public int    penalty;
            // Глагол события (контекст)
            public string actionVerb;
            public string actionVerbPast;
            // Дефолтные подход и опора
            public string defApproach;
            public string defApproachPast;
            public float  defHp, defPow, defSan;
            public string defSupport;
            public int    defReduction;
            // Outcome
            public float  baseChance;
            public float  favorableBonus;
            public string[] favorableKeys;
            public string positiveOutcome;
            public string negativeOutcome;
            public string[] rewardKeys;
            public string[] poolKeys;
            public float  weight;
            // Day & Flags
            public int    dayMin;
            public int    dayMax;
            public bool   isMandatory;
            public string[] requiredFlags;
            public string[] excludedFlags;
            public string setsFlagPos;
            public string setsFlagNeg;
        }

        private static readonly string[] AllWordKeys = {
            "careful","forceful","secret","open","patient","desperate",
            "strength","cunning","gold","word","knowledge","luck"
        };

        // ═════════════════════════════════════════════════════════════════
        //  10-ДНЕВНАЯ КАМПАНИЯ
        // ═════════════════════════════════════════════════════════════════

        private static readonly EventData[] Events = new[]
        {
            // ── ДЕНЬ 1: СБОРЫ ──────────────────────────────────────────

            new EventData {
                key       = "Day1_Shop",
                eventText = "Перед выходом ты заглядываешь в лавку. Старик раскладывает товары на прилавке и щурится: «Бери, путник, дорога длинная.»",
                penalty   = -8,
                actionVerb = "Говорить", actionVerbPast = "говоришь",
                defApproach = "открыто", defApproachPast = "открыто",
                defHp = 1, defPow = 1, defSan = 0,
                defSupport = "словом", defReduction = 3,
                favorableKeys = new[] { "open", "patient", "word", "knowledge" },
                positiveOutcome = "и старик кивает: «Бери, это тебе пригодится». Ты находишь [support] и [support].",
                negativeOutcome = "но старик ворчит и прячет лучшее. Ты берёшь, что осталось.",
                rewardKeys = new[] { "strength", "cunning", "word" },
                poolKeys   = new[] { "strength", "cunning", "word", "open", "patient" },
                weight    = 1f,
                dayMin = 1, dayMax = 1, isMandatory = false,
                baseChance = 1f, favorableBonus = 0f,
                setsFlagPos = "day1_shop", setsFlagNeg = "day1_shop"
            },
            new EventData {
                key       = "Day1_Caravan",
                eventText = "У ворот стоит обоз. Торговец нагружает повозку и зовёт: «Помоги — поделюсь, чем смогу.»",
                penalty   = -8,
                actionVerb = "Помочь", actionVerbPast = "помогаешь",
                defApproach = "открыто", defApproachPast = "открыто",
                defHp = 1, defPow = 0, defSan = 1,
                defSupport = "силой", defReduction = 3,
                favorableKeys = new[] { "forceful", "open", "strength", "word" },
                positiveOutcome = "и торговец протягивает мешок: «Держи, пригодится в пути». Ты находишь [support] и [support].",
                negativeOutcome = "но торговец лишь качает головой. Повозка уезжает без тебя.",
                rewardKeys = new[] { "knowledge", "cunning", "luck" },
                poolKeys   = new[] { "knowledge", "cunning", "luck", "open", "forceful" },
                weight    = 1f,
                dayMin = 1, dayMax = 1, isMandatory = false,
                baseChance = 1f, favorableBonus = 0f,
                setsFlagPos = "day1_caravan", setsFlagNeg = "day1_caravan"
            },

            // ── ДЕНЬ 2: РАЗВИЛКА ──────────────────────────────────────

            new EventData {
                key       = "Day2_Fork",
                eventText = "Тропа раздваивается. Левая ведёт в чащу — тёмную и тихую. Правая — по открытому тракту, где видны следы колёс.",
                penalty   = -12,
                actionVerb = "Выбрать", actionVerbPast = "выбираешь",
                defApproach = "осторожно", defApproachPast = "осторожно",
                defHp = 1, defPow = 1, defSan = 1,
                defSupport = "знанием", defReduction = 0,
                favorableKeys = new[] { "careful", "secret", "knowledge", "cunning" },
                positiveOutcome = "и ты замечаешь старый знак на камне. Находишь [support] и [support].",
                negativeOutcome = "но оба пути выглядят опасно. Ты идёшь по тракту наугад.",
                rewardKeys = new[] { "knowledge", "cunning" },
                poolKeys   = new[] { "knowledge", "cunning", "careful", "secret" },
                weight    = 1f,
                dayMin = 2, dayMax = 2, isMandatory = true,
                baseChance = 0.25f, favorableBonus = 0.3f,
                setsFlagPos = "chose_forest", setsFlagNeg = "chose_road"
            },

            // ── ДЕНЬ 3: ПОСЛЕДСТВИЯ ВЫБОРА ────────────────────────────

            // Ветка A: Лес
            new EventData {
                key       = "Day3_Spirits",
                eventText = "Лесные духи танцуют в лунном свете. Они шепчут: «Путник, принеси дар — и мы откроем путь.»",
                penalty   = -16,
                actionVerb = "Обратиться", actionVerbPast = "обращаешься",
                defApproach = "терпеливо", defApproachPast = "терпеливо",
                defHp = 0, defPow = 1, defSan = 3,
                defSupport = "словом", defReduction = 0,
                favorableKeys = new[] { "patient", "open", "word", "gold", "knowledge" },
                positiveOutcome = "и духи оставляют подарок у корней древа. Ты находишь [support] и [approach].",
                negativeOutcome = "но духи разъярены. Лес замолкает, и ты бредёшь наугад.",
                rewardKeys = new[] { "cunning", "knowledge", "patient" },
                poolKeys   = new[] { "cunning", "knowledge", "word", "patient" },
                weight    = 1f,
                dayMin = 3, dayMax = 3, isMandatory = true,
                baseChance = 0.25f, favorableBonus = 0.3f,
                requiredFlags = new[] { "chose_forest" },
                setsFlagPos = "spirits_gift", setsFlagNeg = "spirits_anger"
            },

            // Ветка B: Тракт
            new EventData {
                key       = "Day3_Ambush",
                eventText = "Засада! Разбойники выскакивают из придорожных кустов. Главарь ухмыляется, поигрывая ножом.",
                penalty   = -18,
                actionVerb = "Встретить", actionVerbPast = "встречаешь",
                defApproach = "напролом", defApproachPast = "напролом",
                defHp = 2, defPow = 2, defSan = 0,
                defSupport = "силой", defReduction = 0,
                favorableKeys = new[] { "forceful", "desperate", "strength", "cunning" },
                positiveOutcome = "и разбойники отступают, бросив добычу. Ты находишь [support] и [support].",
                negativeOutcome = "но разбойники сильнее. Ты еле уносишь ноги.",
                rewardKeys = new[] { "strength", "cunning", "luck" },
                poolKeys   = new[] { "strength", "cunning", "luck", "forceful", "desperate" },
                weight    = 1f,
                dayMin = 3, dayMax = 3, isMandatory = true,
                baseChance = 0.25f, favorableBonus = 0.3f,
                requiredFlags = new[] { "chose_road" },
                setsFlagPos = "ambush_won", setsFlagNeg = "ambush_fled"
            },

            // ── ДЕНЬ 4: СТРАННИК (все ветки сходятся) ─────────────────

            new EventData {
                key       = "Day4_Stranger",
                eventText = "У уцелевшего моста лежит раненый. Он шепчет: «Не ходи туда... руины... ловушка...» Его рука тянется к тебе.",
                penalty   = -15,
                actionVerb = "Помочь", actionVerbPast = "помогаешь",
                defApproach = "осторожно", defApproachPast = "осторожно",
                defHp = 2, defPow = 1, defSan = 0,
                defSupport = "словом", defReduction = 0,
                favorableKeys = new[] { "open", "patient", "word", "gold", "knowledge" },
                positiveOutcome = "и странник благодарит: «Возьми... это поможет». Он даёт тебе [support] и [support].",
                negativeOutcome = "но странник отшатывается. Ты уходишь, не оглядываясь.",
                rewardKeys = new[] { "knowledge", "cunning", "word" },
                poolKeys   = new[] { "knowledge", "cunning", "word", "open", "patient" },
                weight    = 1f,
                dayMin = 4, dayMax = 4, isMandatory = true,
                baseChance = 0.2f, favorableBonus = 0.3f,
                setsFlagPos = "trusted_stranger", setsFlagNeg = "ignored_stranger"
            },

            // ── ДЕНЬ 5: РУИНЫ / БОЛОТО ────────────────────────────────

            new EventData {
                key       = "Day5_Ruins",
                eventText = "Следуя словам странника, ты находишь древние руины. Стены покрыты письменами, а в глубине мерцает свет.",
                penalty   = -16,
                actionVerb = "Исследовать", actionVerbPast = "исследуешь",
                defApproach = "осторожно", defApproachPast = "осторожно",
                defHp = 0, defPow = 1, defSan = 2,
                defSupport = "знанием", defReduction = 0,
                favorableKeys = new[] { "careful", "secret", "knowledge", "cunning" },
                positiveOutcome = "и среди камней ты находишь тайник. Внутри — [support] и [approach].",
                negativeOutcome = "но руины содрогаются. Ты выбегаешь до обвала, потеряв время.",
                rewardKeys = new[] { "knowledge", "cunning", "careful" },
                poolKeys   = new[] { "knowledge", "cunning", "careful", "secret" },
                weight    = 1f,
                dayMin = 5, dayMax = 5, isMandatory = true,
                baseChance = 0.2f, favorableBonus = 0.3f,
                requiredFlags = new[] { "trusted_stranger" },
                setsFlagPos = "ruins_treasure", setsFlagNeg = "ruins_trap"
            },

            new EventData {
                key       = "Day5_Swamp",
                eventText = "Без подсказок ты забрёл в болото. Трясина чавкает под ногами, туман скрывает тропу.",
                penalty   = -18,
                actionVerb = "Пробраться", actionVerbPast = "пробираешься",
                defApproach = "терпеливо", defApproachPast = "терпеливо",
                defHp = 2, defPow = 1, defSan = 2,
                defSupport = "силой", defReduction = 0,
                favorableKeys = new[] { "patient", "careful", "strength", "cunning" },
                positiveOutcome = "и ты находишь твёрдую почву. На кочке лежит [support] и [support].",
                negativeOutcome = "но болото затягивает глубже. Ты выбираешься с трудом.",
                rewardKeys = new[] { "strength", "luck", "cunning" },
                poolKeys   = new[] { "strength", "luck", "cunning", "patient" },
                weight    = 1f,
                dayMin = 5, dayMax = 5, isMandatory = true,
                baseChance = 0.25f, favorableBonus = 0.3f,
                requiredFlags = new[] { "ignored_stranger" },
                setsFlagPos = "swamp_survived", setsFlagNeg = "swamp_lost"
            },

            // ── ДЕНЬ 6: ДЕРЕВНЯ МОРА (все ветки сходятся) ─────────────

            new EventData {
                key       = "Day6_Plague",
                eventText = "Деревня охвачена мором. Старуха у колодца умоляет: «Помоги нам, путник... мы погибаем.»",
                penalty   = -18,
                actionVerb = "Помочь", actionVerbPast = "помогаешь",
                defApproach = "открыто", defApproachPast = "открыто",
                defHp = 2, defPow = 1, defSan = 1,
                defSupport = "словом", defReduction = 0,
                favorableKeys = new[] { "open", "patient", "word", "gold", "knowledge" },
                positiveOutcome = "и знахарка благодарит: «Ты спас нас». Она вручает [support] и [approach].",
                negativeOutcome = "но жители кричат тебе вслед. Мор не отступает.",
                rewardKeys = new[] { "knowledge", "word", "patient" },
                poolKeys   = new[] { "knowledge", "word", "gold", "patient", "open" },
                weight    = 1f,
                dayMin = 6, dayMax = 6, isMandatory = true,
                baseChance = 0.2f, favorableBonus = 0.3f,
                setsFlagPos = "helped_village", setsFlagNeg = "ignored_village"
            },

            // ── ДЕНЬ 7: МОСТ / УЩЕЛЬЕ ────────────────────────────────

            new EventData {
                key       = "Day7_Troll",
                eventText = "Тролль под мостом требует плату. Но сельчане предупредили: «Тролль глуп — его можно обмануть.»",
                penalty   = -20,
                actionVerb = "Пройти", actionVerbPast = "проходишь",
                defApproach = "хитро", defApproachPast = "хитро",
                defHp = 1, defPow = 2, defSan = 1,
                defSupport = "хитростью", defReduction = 0,
                favorableKeys = new[] { "secret", "cunning", "gold", "luck" },
                positiveOutcome = "и тролль хохочет, швыряя тебе [support] и [support]. Мост свободен.",
                negativeOutcome = "но тролль рычит и замахивается. Ты перебегаешь мост бегом.",
                rewardKeys = new[] { "cunning", "luck", "gold" },
                poolKeys   = new[] { "cunning", "luck", "gold", "secret" },
                weight    = 1f,
                dayMin = 7, dayMax = 7, isMandatory = true,
                baseChance = 0.2f, favorableBonus = 0.3f,
                requiredFlags = new[] { "helped_village" },
                setsFlagPos = "troll_paid", setsFlagNeg = "troll_fight"
            },

            new EventData {
                key       = "Day7_Gorge",
                eventText = "В узком ущелье тебя поджидают мародёры. Они знали, что ты пройдёшь здесь — кто-то донёс.",
                penalty   = -22,
                actionVerb = "Прорваться", actionVerbPast = "прорываешься",
                defApproach = "отчаянно", defApproachPast = "отчаянно",
                defHp = 2, defPow = 2, defSan = 1,
                defSupport = "силой", defReduction = 0,
                favorableKeys = new[] { "forceful", "desperate", "strength", "luck" },
                positiveOutcome = "и мародёры бегут, бросив награбленное. Среди вещей — [support] и [support].",
                negativeOutcome = "но мародёры сильнее. Ты вырываешься с потерями.",
                rewardKeys = new[] { "strength", "luck", "cunning" },
                poolKeys   = new[] { "strength", "luck", "cunning", "forceful", "desperate" },
                weight    = 1f,
                dayMin = 7, dayMax = 7, isMandatory = true,
                baseChance = 0.2f, favorableBonus = 0.3f,
                requiredFlags = new[] { "ignored_village" },
                setsFlagPos = "gorge_escaped", setsFlagNeg = "gorge_wounded"
            },

            // ── ДЕНЬ 8: ГОЛЕМ (все ветки сходятся) ────────────────────

            new EventData {
                key       = "Day8_Golem",
                eventText = "Каменный голем преграждает перевал. Его глаза пылают алым. Земля дрожит под его шагами.",
                penalty   = -24,
                actionVerb = "Остановить", actionVerbPast = "останавливаешь",
                defApproach = "напролом", defApproachPast = "напролом",
                defHp = 2, defPow = 2, defSan = 1,
                defSupport = "силой", defReduction = 0,
                favorableKeys = new[] { "forceful", "desperate", "strength", "cunning", "knowledge" },
                positiveOutcome = "и голем рассыпается. Среди обломков — [support] и [support].",
                negativeOutcome = "но голем наносит удар. Ты проскакиваешь, однако ранен.",
                rewardKeys = new[] { "strength", "cunning", "knowledge" },
                poolKeys   = new[] { "strength", "cunning", "knowledge", "forceful", "desperate" },
                weight    = 1f,
                dayMin = 8, dayMax = 8, isMandatory = true,
                baseChance = 0.15f, favorableBonus = 0.3f,
                setsFlagPos = "golem_fallen", setsFlagNeg = "golem_wounded"
            },

            // ── ДЕНЬ 9: ПОРТАЛ / ГОЛОС ────────────────────────────────

            new EventData {
                key       = "Day9_Portal",
                eventText = "Мерцающий портал висит в воздухе. Нечто внутри резонирует с твоим прошлым.",
                penalty   = -20,
                actionVerb = "Войти", actionVerbPast = "входишь",
                defApproach = "осторожно", defApproachPast = "осторожно",
                defHp = 1, defPow = 1, defSan = 3,
                defSupport = "знанием", defReduction = 0,
                favorableKeys = new[] { "careful", "patient", "knowledge", "word", "gold" },
                positiveOutcome = "и портал схлопывается, выбросив [support] и [approach]. Тишина.",
                negativeOutcome = "но портал втягивает часть твоей силы и исчезает.",
                rewardKeys = new[] { "knowledge", "cunning", "patient" },
                poolKeys   = new[] { "knowledge", "cunning", "patient", "careful" },
                weight    = 1f,
                dayMin = 9, dayMax = 9, isMandatory = true,
                baseChance = 0.15f, favorableBonus = 0.3f,
                requiredFlags = new[] { "spirits_gift" },
                setsFlagPos = "portal_crossed", setsFlagNeg = "portal_drained"
            },

            new EventData {
                key       = "Day9_Voice",
                eventText = "Голос из-под земли обещает силу: «Дай мне что-нибудь... и я дам тебе гораздо больше.»",
                penalty   = -20,
                actionVerb = "Ответить", actionVerbPast = "отвечаешь",
                defApproach = "осторожно", defApproachPast = "осторожно",
                defHp = 1, defPow = 1, defSan = 3,
                defSupport = "словом", defReduction = 0,
                favorableKeys = new[] { "careful", "patient", "word", "knowledge", "gold" },
                positiveOutcome = "и голос стихает. На земле — [support] и [approach]. Сделка заключена.",
                negativeOutcome = "но голос хохочет и замолкает. Земля дрожит под ногами.",
                rewardKeys = new[] { "cunning", "knowledge", "careful" },
                poolKeys   = new[] { "cunning", "knowledge", "careful", "patient" },
                weight    = 1f,
                dayMin = 9, dayMax = 9, isMandatory = true,
                baseChance = 0.15f, favorableBonus = 0.3f,
                excludedFlags = new[] { "spirits_gift" },
                setsFlagPos = "voice_deal", setsFlagNeg = "voice_rejected"
            },

            // ── ДЕНЬ 10: ФИНАЛ ────────────────────────────────────────

            new EventData {
                key       = "Day10_Final",
                eventText = "Конец пути. Перед тобой — то, к чему ты шёл. Тени прошлого стоят за спиной. Всё, что ты сделал, привело тебя сюда.",
                penalty   = -25,
                actionVerb = "Шагнуть", actionVerbPast = "шагаешь",
                defApproach = "открыто", defApproachPast = "открыто",
                defHp = 2, defPow = 2, defSan = 2,
                defSupport = "силой", defReduction = 0,
                favorableKeys = new[] { "careful", "open", "forceful", "desperate", "strength", "knowledge", "cunning", "word", "gold", "luck" },
                positiveOutcome = "и свет пробивается сквозь тьму. Ты выжил. Путь окончен.",
                negativeOutcome = "но тьма поглощает тебя. Ты становишься частью дороги, которой шёл.",
                rewardKeys = Array.Empty<string>(),
                poolKeys   = new[] { "knowledge", "cunning", "strength" },
                weight    = 1f,
                dayMin = 10, dayMax = 10, isMandatory = true,
                baseChance = 0.1f, favorableBonus = 0.25f,
                setsFlagPos = "survived_final", setsFlagNeg = "lost_final"
            },
        };

        // ── Меню ────────────────────────────────────────────────────────────

        [MenuItem("Tools/Story/Generate Event Assets")]
        public static void Generate()
        {
            const string folder  = "Assets/_Project/Data/Events";
            CreateFolder(folder);

            var wordMap = new Dictionary<string, WordSO>();
            foreach (var key in AllWordKeys)
            {
                string path = $"Assets/_Project/Data/Words/Word_{key}.asset";
                var w = AssetDatabase.LoadAssetAtPath<WordSO>(path);
                if (w != null) wordMap[key] = w;
                else Debug.LogWarning($"[EventAssetGenerator] WordSO не найден: {path}");
            }

            var dbGuids = AssetDatabase.FindAssets("t:EventDatabaseSO");
            EventDatabaseSO db = dbGuids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<EventDatabaseSO>(
                    AssetDatabase.GUIDToAssetPath(dbGuids[0]))
                : null;

            if (db != null) db.events.Clear();

            foreach (var data in Events)
            {
                string path = $"{folder}/Event_{data.key}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<EventSO>(path);
                var so = existing != null ? existing : ScriptableObject.CreateInstance<EventSO>();

                so.eventText    = data.eventText;
                so.totalPenalty = data.penalty;

                so.actionVerb     = data.actionVerb;
                so.actionVerbPast = data.actionVerbPast ?? "";

                so.defaultApproachAdverb     = data.defApproach;
                so.defaultApproachAdverbPast = data.defApproachPast ?? "";
                so.defaultHpWeight           = data.defHp;
                so.defaultPowWeight          = data.defPow;
                so.defaultSanWeight          = data.defSan;

                so.defaultSupportAdverb    = data.defSupport;
                so.defaultPenaltyReduction = data.defReduction;

                so.baseChance     = data.baseChance;
                so.favorableBonus = data.favorableBonus;
                so.favorableWords.Clear();
                if (data.favorableKeys != null)
                    so.favorableWords.AddRange(data.favorableKeys);
                so.positiveOutcomeText = data.positiveOutcome;
                so.negativeOutcomeText = data.negativeOutcome;

                so.weight = data.weight;

                // Day & Flags
                so.dayMin       = data.dayMin;
                so.dayMax       = data.dayMax;
                so.isMandatory  = data.isMandatory;
                so.requiredFlags  = data.requiredFlags ?? Array.Empty<string>();
                so.excludedFlags  = data.excludedFlags ?? Array.Empty<string>();
                so.setsFlagOnPositive = data.setsFlagPos ?? "";
                so.setsFlagOnNegative = data.setsFlagNeg ?? "";

                so.rewardWordPool.Clear();
                if (data.rewardKeys != null)
                    foreach (var k in data.rewardKeys)
                        if (wordMap.TryGetValue(k, out var rw))
                            so.rewardWordPool.Add(rw);

                so.eventWordPool.Clear();
                if (data.poolKeys != null)
                    foreach (var k in data.poolKeys)
                        if (wordMap.TryGetValue(k, out var pw))
                            so.eventWordPool.Add(pw);

                if (existing == null)
                    AssetDatabase.CreateAsset(so, path);
                else
                    EditorUtility.SetDirty(so);

                if (db != null) db.events.Add(so);
            }

            if (db != null) EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[EventAssetGenerator] Создано/обновлено {Events.Length} событий в {folder}");
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static void CreateFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
