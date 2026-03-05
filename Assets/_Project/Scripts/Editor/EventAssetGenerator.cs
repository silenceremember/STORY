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
    /// </summary>
    public static class EventAssetGenerator
    {
        private struct EventData
        {
            public string key;
            public string eventText;
            public int    penalty;
            public string defStart;
            public string defPhrasePast;
            public float  defHp, defPow, defSan;
            public string defEnd;
            public int    defReduction;
            // Outcome
            public bool   defaultPositive;
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
            "hurl","throw","give","show","use","hide","toss","offer","grab","raise",
            "sword","shield","torch","medicine","map","rations","amulet","poison","book","dagger"
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
                penalty   = -5,
                defStart  = "Осмотреть", defPhrasePast = "осматриваешь",
                defHp = 1, defPow = 1, defSan = 0,
                defEnd    = "товары", defReduction = 3,
                favorableKeys = new[] { "show", "use", "grab", "offer" },
                positiveOutcome = "и старик кивает: «Бери, это тебе пригодится». Ты находишь [noun].",
                negativeOutcome = "но старик ворчит и прячет лучшее. Ты берёшь, что осталось.",
                rewardKeys = new[] { "sword", "shield" },
                poolKeys   = new[] { "sword", "shield", "torch" },
                weight    = 1f,
                dayMin = 1, dayMax = 1, isMandatory = false,
                defaultPositive = true,
                setsFlagPos = "day1_shop", setsFlagNeg = "day1_shop"
            },
            new EventData {
                key       = "Day1_Caravan",
                eventText = "У ворот стоит обоз. Торговец нагружает повозку и зовёт: «Помоги — поделюсь, чем смогу.»",
                penalty   = -5,
                defStart  = "Осмотреть", defPhrasePast = "осматриваешь",
                defHp = 1, defPow = 0, defSan = 1,
                defEnd    = "повозку", defReduction = 3,
                favorableKeys = new[] { "grab", "use", "give", "offer" },
                positiveOutcome = "и торговец протягивает мешок: «Держи, пригодится в пути». Ты находишь [noun].",
                negativeOutcome = "но торговец лишь качает головой. Повозка уезжает без тебя.",
                rewardKeys = new[] { "torch", "rations" },
                poolKeys   = new[] { "torch", "rations", "map" },
                weight    = 1f,
                dayMin = 1, dayMax = 1, isMandatory = false,
                defaultPositive = true,
                setsFlagPos = "day1_caravan", setsFlagNeg = "day1_caravan"
            },

            // ── ДЕНЬ 2: РАЗВИЛКА ──────────────────────────────────────

            new EventData {
                key       = "Day2_Fork",
                eventText = "Тропа раздваивается. Левая ведёт в чащу — тёмную и тихую. Правая — по открытому тракту, где видны следы колёс.",
                penalty   = -8,
                defStart  = "Осмотреть", defPhrasePast = "осматриваешь",
                defHp = 1, defPow = 1, defSan = 1,
                defEnd    = "тропу", defReduction = 0,
                favorableKeys = new[] { "map", "torch", "show", "use" },
                positiveOutcome = "и ты замечаешь старую карту на камне. Чаща манит тайнами.",
                negativeOutcome = "но оба пути выглядят опасно. Ты идёшь по тракту наугад.",
                rewardKeys = new[] { "map" },
                poolKeys   = new[] { "map", "torch" },
                weight    = 1f,
                dayMin = 2, dayMax = 2, isMandatory = true,
                setsFlagPos = "chose_forest", setsFlagNeg = "chose_road"
            },

            // ── ДЕНЬ 3: ПОСЛЕДСТВИЯ ВЫБОРА ────────────────────────────

            // Ветка A: Лес
            new EventData {
                key       = "Day3_Spirits",
                eventText = "Лесные духи танцуют в лунном свете. Они шепчут: «Путник, принеси дар — и мы откроем путь.»",
                penalty   = -11,
                defStart  = "Осмотреть", defPhrasePast = "осматриваешь",
                defHp = 0, defPow = 1, defSan = 3,
                defEnd    = "духов", defReduction = 0,
                favorableKeys = new[] { "amulet", "book", "torch", "show", "offer", "give" },
                positiveOutcome = "и духи оставляют подарок у корней древа. Ты находишь [noun].",
                negativeOutcome = "но духи разъярены. Лес замолкает, и ты бредёшь наугад.",
                rewardKeys = new[] { "amulet" },
                poolKeys   = new[] { "amulet", "book", "torch" },
                weight    = 1f,
                dayMin = 3, dayMax = 3, isMandatory = true,
                requiredFlags = new[] { "chose_forest" },
                setsFlagPos = "spirits_gift", setsFlagNeg = "spirits_anger"
            },

            // Ветка B: Тракт
            new EventData {
                key       = "Day3_Ambush",
                eventText = "Засада! Разбойники выскакивают из придорожных кустов. Главарь ухмыляется, поигрывая ножом.",
                penalty   = -13,
                defStart  = "Обойти", defPhrasePast = "обходишь",
                defHp = 2, defPow = 2, defSan = 0,
                defEnd    = "разбойников", defReduction = 0,
                favorableKeys = new[] { "sword", "dagger", "shield", "hurl", "throw", "grab", "toss" },
                positiveOutcome = "и разбойники отступают, бросив добычу. Ты находишь [noun].",
                negativeOutcome = "но разбойники лишь злятся. Ты еле уносишь ноги.",
                rewardKeys = new[] { "dagger" },
                poolKeys   = new[] { "dagger", "sword", "shield" },
                weight    = 1f,
                dayMin = 3, dayMax = 3, isMandatory = true,
                requiredFlags = new[] { "chose_road" },
                setsFlagPos = "ambush_won", setsFlagNeg = "ambush_fled"
            },

            // ── ДЕНЬ 4: СТРАННИК (все ветки сходятся) ─────────────────

            new EventData {
                key       = "Day4_Stranger",
                eventText = "У уцелевшего моста лежит раненый. Он шепчет: «Не ходи туда... руины... ловушка...» Его рука тянется к тебе.",
                penalty   = -10,
                defStart  = "Осмотреть", defPhrasePast = "осматриваешь",
                defHp = 2, defPow = 1, defSan = 0,
                defEnd    = "странника", defReduction = 0,
                favorableKeys = new[] { "medicine", "rations", "give", "offer", "show" },
                positiveOutcome = "и странник благодарит: «Возьми... это поможет». Он даёт тебе [noun].",
                negativeOutcome = "но странник отшатывается. Ты уходишь, не оглядываясь.",
                rewardKeys = new[] { "medicine", "book" },
                poolKeys   = new[] { "medicine", "rations", "book" },
                weight    = 1f,
                dayMin = 4, dayMax = 4, isMandatory = true,
                setsFlagPos = "trusted_stranger", setsFlagNeg = "ignored_stranger"
            },

            // ── ДЕНЬ 5: РУИНЫ / БОЛОТО ────────────────────────────────

            // Помог страннику → руины
            new EventData {
                key       = "Day5_Ruins",
                eventText = "Следуя словам странника, ты находишь древние руины. Стены покрыты письменами, а в глубине мерцает свет.",
                penalty   = -10,
                defStart  = "Изучить", defPhrasePast = "изучаешь",
                defHp = 0, defPow = 1, defSan = 2,
                defEnd    = "письмена", defReduction = 0,
                favorableKeys = new[] { "book", "map", "torch", "show", "use" },
                positiveOutcome = "и среди камней ты находишь тайник. Внутри — [noun].",
                negativeOutcome = "но руины содрогаются. Ты выбегаешь до обвала, потеряв время.",
                rewardKeys = new[] { "book", "amulet" },
                poolKeys   = new[] { "book", "map", "torch" },
                weight    = 1f,
                dayMin = 5, dayMax = 5, isMandatory = true,
                requiredFlags = new[] { "trusted_stranger" },
                setsFlagPos = "ruins_treasure", setsFlagNeg = "ruins_trap"
            },

            // Проигнорировал странника → болото
            new EventData {
                key       = "Day5_Swamp",
                eventText = "Без подсказок ты забрёл в болото. Трясина чавкает под ногами, туман скрывает тропу.",
                penalty   = -14,
                defStart  = "Осмотреть", defPhrasePast = "осматриваешь",
                defHp = 2, defPow = 1, defSan = 2,
                defEnd    = "тропу", defReduction = 0,
                favorableKeys = new[] { "torch", "map", "use", "raise", "grab" },
                positiveOutcome = "и ты находишь твёрдую почву. На кочке лежит [noun].",
                negativeOutcome = "но болото затягивает глубже. Ты выбираешься с трудом.",
                rewardKeys = new[] { "rations", "medicine" },
                poolKeys   = new[] { "rations", "medicine", "torch" },
                weight    = 1f,
                dayMin = 5, dayMax = 5, isMandatory = true,
                requiredFlags = new[] { "ignored_stranger" },
                setsFlagPos = "swamp_survived", setsFlagNeg = "swamp_lost"
            },

            // ── ДЕНЬ 6: ДЕРЕВНЯ МОРА (все ветки сходятся) ─────────────

            new EventData {
                key       = "Day6_Plague",
                eventText = "Деревня охвачена мором. Старуха у колодца умоляет: «Помоги нам, путник... мы погибаем.»",
                penalty   = -13,
                defStart  = "Осмотреть", defPhrasePast = "осматриваешь",
                defHp = 2, defPow = 1, defSan = 1,
                defEnd    = "деревню", defReduction = 0,
                favorableKeys = new[] { "medicine", "rations", "give", "offer" },
                positiveOutcome = "и знахарка благодарит: «Ты спас нас». Она вручает [noun].",
                negativeOutcome = "но жители кричат тебе вслед. Мор не отступает.",
                rewardKeys = new[] { "medicine", "rations" },
                poolKeys   = new[] { "medicine", "rations", "book" },
                weight    = 1f,
                dayMin = 6, dayMax = 6, isMandatory = true,
                setsFlagPos = "helped_village", setsFlagNeg = "ignored_village"
            },

            // ── ДЕНЬ 7: МОСТ / УЩЕЛЬЕ ────────────────────────────────

            // Помог деревне → мост тролля
            new EventData {
                key       = "Day7_Troll",
                eventText = "Тролль под мостом требует плату. Но сельчане предупредили: «Тролль глуп — его можно обмануть.»",
                penalty   = -12,
                defStart  = "Обойти", defPhrasePast = "обходишь",
                defHp = 1, defPow = 2, defSan = 1,
                defEnd    = "тролля", defReduction = 0,
                favorableKeys = new[] { "rations", "amulet", "give", "offer", "toss", "show" },
                positiveOutcome = "и тролль хохочет, швыряя тебе [noun]. Мост свободен.",
                negativeOutcome = "но тролль рычит и замахивается. Ты перебегаешь мост бегом.",
                rewardKeys = new[] { "shield", "sword" },
                poolKeys   = new[] { "shield", "sword", "rations" },
                weight    = 1f,
                dayMin = 7, dayMax = 7, isMandatory = true,
                requiredFlags = new[] { "helped_village" },
                setsFlagPos = "troll_paid", setsFlagNeg = "troll_fight"
            },

            // Не помог деревне → засада мародёров
            new EventData {
                key       = "Day7_Gorge",
                eventText = "В узком ущелье тебя поджидают мародёры. Они знали, что ты пройдёшь здесь — кто-то донёс.",
                penalty   = -16,
                defStart  = "Осмотреть", defPhrasePast = "осматриваешь",
                defHp = 2, defPow = 2, defSan = 1,
                defEnd    = "укрытие", defReduction = 0,
                favorableKeys = new[] { "sword", "dagger", "shield", "hurl", "throw", "grab", "toss" },
                positiveOutcome = "и мародёры бегут, бросив награбленное. Среди вещей — [noun].",
                negativeOutcome = "но мародёры сильнее. Ты вырываешься с потерями.",
                rewardKeys = new[] { "dagger", "poison" },
                poolKeys   = new[] { "dagger", "poison", "sword" },
                weight    = 1f,
                dayMin = 7, dayMax = 7, isMandatory = true,
                requiredFlags = new[] { "ignored_village" },
                setsFlagPos = "gorge_escaped", setsFlagNeg = "gorge_wounded"
            },

            // ── ДЕНЬ 8: ГОЛЕМ (все ветки сходятся) ────────────────────

            new EventData {
                key       = "Day8_Golem",
                eventText = "Каменный голем преграждает перевал. Его глаза пылают алым. Земля дрожит под его шагами.",
                penalty   = -18,
                defStart  = "Обойти", defPhrasePast = "обходишь",
                defHp = 2, defPow = 2, defSan = 1,
                defEnd    = "голема", defReduction = 0,
                favorableKeys = new[] { "sword", "dagger", "hurl", "throw", "toss", "grab" },
                positiveOutcome = "и голем рассыпается. Среди обломков — [noun].",
                negativeOutcome = "но голем наносит удар. Ты проскакиваешь, однако ранен.",
                rewardKeys = new[] { "shield", "amulet" },
                poolKeys   = new[] { "shield", "amulet", "sword" },
                weight    = 1f,
                dayMin = 8, dayMax = 8, isMandatory = true,
                setsFlagPos = "golem_fallen", setsFlagNeg = "golem_wounded"
            },

            // ── ДЕНЬ 9: ПОРТАЛ / ГОЛОС ────────────────────────────────

            // Есть дар духов → портал
            new EventData {
                key       = "Day9_Portal",
                eventText = "Мерцающий портал висит в воздухе. Амулет духов пульсирует — он резонирует с порталом.",
                penalty   = -15,
                defStart  = "Осмотреть", defPhrasePast = "осматриваешь",
                defHp = 1, defPow = 1, defSan = 3,
                defEnd    = "портал", defReduction = 0,
                favorableKeys = new[] { "amulet", "book", "use", "show", "raise" },
                positiveOutcome = "и портал схлопывается, выбросив [noun]. Амулет тускнеет.",
                negativeOutcome = "но портал втягивает часть твоей силы и исчезает.",
                rewardKeys = new[] { "poison", "book" },
                poolKeys   = new[] { "poison", "book", "amulet" },
                weight    = 1f,
                dayMin = 9, dayMax = 9, isMandatory = true,
                requiredFlags = new[] { "spirits_gift" },
                setsFlagPos = "portal_crossed", setsFlagNeg = "portal_drained"
            },

            // Нет дара духов → голос из-под земли
            new EventData {
                key       = "Day9_Voice",
                eventText = "Голос из-под земли обещает силу: «Дай мне что-нибудь... и я дам тебе гораздо больше.»",
                penalty   = -14,
                defStart  = "Осмотреть", defPhrasePast = "осматриваешь",
                defHp = 1, defPow = 1, defSan = 3,
                defEnd    = "разлом", defReduction = 0,
                favorableKeys = new[] { "amulet", "book", "use", "show", "raise" },
                positiveOutcome = "и голос стихает. На земле — [noun]. Сделка заключена.",
                negativeOutcome = "но голос хохочет и замолкает. Земля дрожит под ногами.",
                rewardKeys = new[] { "poison", "amulet" },
                poolKeys   = new[] { "poison", "amulet", "book" },
                weight    = 1f,
                dayMin = 9, dayMax = 9, isMandatory = true,
                excludedFlags = new[] { "spirits_gift" },
                setsFlagPos = "voice_deal", setsFlagNeg = "voice_rejected"
            },

            // ── ДЕНЬ 10: ФИНАЛ ────────────────────────────────────────

            new EventData {
                key       = "Day10_Final",
                eventText = "Конец пути. Перед тобой — то, к чему ты шёл. Тени прошлого стоят за спиной. Всё, что ты сделал, привело тебя сюда.",
                penalty   = -20,
                defStart  = "Осмотреть", defPhrasePast = "осматриваешь",
                defHp = 2, defPow = 2, defSan = 2,
                defEnd    = "тьму", defReduction = 0,
                favorableKeys = new[] { "sword", "amulet", "book", "shield", "hurl", "throw", "use", "raise", "show" },
                positiveOutcome = "и свет пробивается сквозь тьму. Ты выжил. Путь окончен.",
                negativeOutcome = "но тьма поглощает тебя. Ты становишься частью дороги, которой шёл.",
                rewardKeys = Array.Empty<string>(),
                poolKeys   = new[] { "sword", "amulet", "book" },
                weight    = 1f,
                dayMin = 10, dayMax = 10, isMandatory = true,
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

                so.eventText     = data.eventText;
                so.totalPenalty  = data.penalty;

                so.defaultPhraseStart    = data.defStart;
                so.defaultPhrasePast     = data.defPhrasePast ?? "";
                so.defaultHpWeight       = data.defHp;
                so.defaultPowWeight      = data.defPow;
                so.defaultSanWeight      = data.defSan;

                so.defaultPhraseEnd        = data.defEnd;
                so.defaultPenaltyReduction = data.defReduction;

                so.defaultPositive = data.defaultPositive;
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
