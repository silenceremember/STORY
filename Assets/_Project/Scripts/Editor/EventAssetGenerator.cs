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
        private struct SynergyData
        {
            public WordArchetype approach;
            public WordArchetype support;
            public float         bonusChance;
            public string        positiveText;
            public string        negativeText;
        }

        private struct EventData
        {
            public string key;
            public string eventText;
            public int    penalty;
            public string actionVerb;
            public string actionVerbPast;
            public string defApproach;
            public string defApproachPast;
            public float  defHp, defPow, defSan;
            public string defSupport;
            public int    defReduction;
            public float  baseChance;
            public string positiveOutcome;
            public string negativeOutcome;
            public string[] rewardKeys;
            public string[] poolKeys;
            public float  weight;
            public int    dayMin;
            public int    dayMax;
            public bool   isMandatory;
            public string[] requiredFlags;
            public string[] excludedFlags;
            public string setsFlagPos;
            public string setsFlagNeg;
            public SynergyData[] synergies;
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
                positiveOutcome = "и старик кивает: «Бери, это тебе пригодится». Ты находишь [support] и [support].",
                negativeOutcome = "но старик ворчит и прячет лучшее. Ты берёшь, что осталось.",
                rewardKeys = new[] { "strength", "cunning", "word" },
                poolKeys   = new[] { "strength", "cunning", "word", "open", "patient" },
                weight    = 1f,
                dayMin = 1, dayMax = 1, isMandatory = false,
                baseChance = 0.5f,
                setsFlagPos = "day1_shop", setsFlagNeg = "day1_shop",
                synergies = new[] {
                    new SynergyData { approach=WordArchetype.Social, support=WordArchetype.Social, bonusChance=0.25f,
                        positiveText="и старик расцветает: «Знаешь, я нашёл кое-что для тебя». Ты находишь [support] и [support].",
                        negativeText="но старик качает головой: «Слова дешевы— плати золотом.»" },
                    new SynergyData { approach=WordArchetype.Mental, support=WordArchetype.Mental, bonusChance=0.15f,
                        positiveText="и старик выдаёт всё, что просите. Ты находишь [support] и [support].",
                        negativeText="но старик видит хитреца насквозь. «Я не вчера родился.»" },
                    new SynergyData { approach=WordArchetype.Physical, support=WordArchetype.Physical, bonusChance=-0.15f,
                        positiveText="и старик поспешно соглашается. Ты находишь [support] и [support].",
                        negativeText="но старик хлопает по прилавке: «Бруталы мне не нужны.»" }
                }
            },
            new EventData {
                key       = "Day1_Caravan",
                eventText = "У ворот стоит обоз. Торговец нагружает повозку и зовёт: «Помоги — поделюсь, чем смогу.»",
                penalty   = -8,
                actionVerb = "Помочь", actionVerbPast = "помогаешь",
                defApproach = "открыто", defApproachPast = "открыто",
                defHp = 1, defPow = 0, defSan = 1,
                defSupport = "силой", defReduction = 3,
                positiveOutcome = "и торговец протягивает мешок: «Держи, пригодится в пути». Ты находишь [support] и [support].",
                negativeOutcome = "но торговец лишь качает головой. Повозка уезжает без тебя.",
                rewardKeys = new[] { "knowledge", "cunning", "luck" },
                poolKeys   = new[] { "knowledge", "cunning", "luck", "open", "forceful" },
                weight    = 1f,
                dayMin = 1, dayMax = 1, isMandatory = false,
                baseChance = 0.5f,
                setsFlagPos = "day1_caravan", setsFlagNeg = "day1_caravan",
                synergies = new[] {
                    new SynergyData { approach=WordArchetype.Physical, support=WordArchetype.Physical, bonusChance=0.25f,
                        positiveText="и торговец шлёпает по плечу: «Иногда нужна простая сила!» Ты находишь [support] и [support].",
                        negativeText="но торговец подозрительно косится. «Сколько вещей потерял?»" },
                    new SynergyData { approach=WordArchetype.Social, support=WordArchetype.Social, bonusChance=0.1f,
                        positiveText="и торговец улыбается: «Хороший народ.» Ты находишь [support] и [support].",
                        negativeText="но торговец только кивает. «Помогать каждый может.»" },
                    new SynergyData { approach=WordArchetype.Mental, support=WordArchetype.Physical, bonusChance=-0.1f,
                        positiveText="и торговец выдаёт меньше, чем мог бы. Ты находишь [support].",
                        negativeText="но торговец знает: работа без души недостаточна." }
                }
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
                positiveOutcome = "и ты замечаешь старый знак на камне. Находишь [support] и [support].",
                negativeOutcome = "но оба пути выглядят опасно. Ты идёшь по тракту наугад.",
                rewardKeys = new[] { "knowledge", "cunning" },
                poolKeys   = new[] { "knowledge", "cunning", "careful", "secret" },
                weight    = 1f,
                dayMin = 2, dayMax = 2, isMandatory = true,
                baseChance = 0.3f,
                setsFlagPos = "chose_forest", setsFlagNeg = "chose_road",
                synergies = new[] {
                    new SynergyData { approach=WordArchetype.Mental, support=WordArchetype.Mental, bonusChance=0.25f,
                        positiveText="и ты замечаешь скрытый ориентир: мох на камне указывает в чащу. Ты находишь [support] и [support].",
                        negativeText="но знания не спасают от неопределённости. Оба пути кажутся одинаково опасными." },
                    new SynergyData { approach=WordArchetype.Social, support=WordArchetype.Mental, bonusChance=0.15f,
                        positiveText="и ты спрашиваешь старика на обочине. Он указывает на чащу. Ты находишь [support] и [support].",
                        negativeText="но старик лишь пожимает плечами. «Я здесь не жуву.»" },
                    new SynergyData { approach=WordArchetype.Physical, support=WordArchetype.Physical, bonusChance=-0.15f,
                        positiveText="и ты пробиваешься в чащу напрямую. Дорога есть, но сложна. Ты находишь [support].",
                        negativeText="но сила не помогает выбрать путь. Ты брёдёшь по тракту наугад." }
                }
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
                positiveOutcome = "и духи оставляют подарок у корней древа. Ты находишь [support] и [approach].",
                negativeOutcome = "но духи разъярены. Лес замолкает, и ты бредёшь наугад.",
                rewardKeys = new[] { "cunning", "knowledge", "patient" },
                poolKeys   = new[] { "cunning", "knowledge", "word", "patient" },
                weight    = 1f,
                dayMin = 3, dayMax = 3, isMandatory = true,
                baseChance = 0.3f,
                requiredFlags = new[] { "chose_forest" },
                setsFlagPos = "spirits_gift", setsFlagNeg = "spirits_anger",
                synergies = new[] {
                    new SynergyData { approach=WordArchetype.Social, support=WordArchetype.Social, bonusChance=0.25f,
                        positiveText="и духи принимают дар. «Ты говоришь насим  языке.» Ты находишь [support] и [approach].",
                        negativeText="но духи чуют лицемерие. «Твои слова пусты.» Лес замолкает." },
                    new SynergyData { approach=WordArchetype.Mental, support=WordArchetype.Social, bonusChance=0.15f,
                        positiveText="и духи кивают: «Мудро. Редко.» Ты находишь [support] и [approach].",
                        negativeText="но разум без души не работает с духами. Лес молчит." },
                    new SynergyData { approach=WordArchetype.Physical, support=WordArchetype.Physical, bonusChance=-0.2f,
                        positiveText="и духи испуганы силой. Они оставляют что-то. Ты находишь [support].",
                        negativeText="но духи разъярены грубостью. Лес замолкает и ты бредёшь наугад." }
                }
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
                positiveOutcome = "и разбойники отступают, бросив добычу. Ты находишь [support] и [support].",
                negativeOutcome = "но разбойники сильнее. Ты еле уносишь ноги.",
                rewardKeys = new[] { "strength", "cunning", "luck" },
                poolKeys   = new[] { "strength", "cunning", "luck", "forceful", "desperate" },
                weight    = 1f,
                dayMin = 3, dayMax = 3, isMandatory = true,
                baseChance = 0.3f,
                requiredFlags = new[] { "chose_road" },
                setsFlagPos = "ambush_won", setsFlagNeg = "ambush_fled",
                synergies = new[] {
                    new SynergyData { approach=WordArchetype.Physical, support=WordArchetype.Physical, bonusChance=0.25f,
                        positiveText="и разбойники разбегаются. «Не наш день.» Ты находишь [support] и [support].",
                        negativeText="но сила без тактики истощается. Разбойники зажимают кольцо." },
                    new SynergyData { approach=WordArchetype.Mental, support=WordArchetype.Physical, bonusChance=0.15f,
                        positiveText="и разбойники отступают. Добыча — твоя. Ты находишь [support] и [support].",
                        negativeText="но хитрость не заменяет свежих сил. Ты еле уносишь ноги." },
                    new SynergyData { approach=WordArchetype.Social, support=WordArchetype.Social, bonusChance=-0.15f,
                        positiveText="и главарь жмётся на мгновение. Слова помогли. Ты находишь [support].",
                        negativeText="но слова не останавливают бандитов. Ты еле уносишь ноги." }
                }
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
                positiveOutcome = "и странник благодарит: «Возьми... это поможет». Он даёт тебе [support] и [support].",
                negativeOutcome = "но странник отшатывается. Ты уходишь, не оглядываясь.",
                rewardKeys = new[] { "knowledge", "cunning", "word" },
                poolKeys   = new[] { "knowledge", "cunning", "word", "open", "patient" },
                weight    = 1f,
                dayMin = 4, dayMax = 4, isMandatory = true,
                baseChance = 0.3f,
                setsFlagPos = "trusted_stranger", setsFlagNeg = "ignored_stranger",
                synergies = new[] {
                    new SynergyData { approach=WordArchetype.Social, support=WordArchetype.Social, bonusChance=0.25f,
                        positiveText="и странник плачет от благодарности. «Ты спас мою жизнь.» Он даёт [support] и [support].",
                        negativeText="но странник отшатывается: «Ты не веришь мне, да я чувствую.»" },
                    new SynergyData { approach=WordArchetype.Mental, support=WordArchetype.Mental, bonusChance=0.15f,
                        positiveText="и странник доверяет: «Ты не такой, как все.» Он даёт [support] и [support].",
                        negativeText="но странник чует прагматизм. «Ты хочешь награды, а не помочь.»" },
                    new SynergyData { approach=WordArchetype.Physical, support=WordArchetype.Physical, bonusChance=-0.2f,
                        positiveText="и странник связывает раны и встаёт. Он оставляет [support].",
                        negativeText="но раненый испуган. Грубая сила не лечит." }
                }
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
                positiveOutcome = "и среди камней ты находишь тайник. Внутри — [support] и [approach].",
                negativeOutcome = "но руины содрогаются. Ты выбегаешь до обвала, потеряв время.",
                rewardKeys = new[] { "knowledge", "cunning", "careful" },
                poolKeys   = new[] { "knowledge", "cunning", "careful", "secret" },
                weight    = 1f,
                dayMin = 5, dayMax = 5, isMandatory = true,
                baseChance = 0.3f,
                requiredFlags = new[] { "trusted_stranger" },
                setsFlagPos = "ruins_treasure", setsFlagNeg = "ruins_trap",
                synergies = new[] {
                    new SynergyData { approach=WordArchetype.Mental, support=WordArchetype.Mental, bonusChance=0.25f,
                        positiveText="и письмена оживают. Ты читаешь тайное слово и находишь [support] и [approach].",
                        negativeText="но знание не спасает от ловушки. Ты выбегаешь из обвала." },
                    new SynergyData { approach=WordArchetype.Social, support=WordArchetype.Mental, bonusChance=0.1f,
                        positiveText="и интуиция ведёт тебя. Ты находишь [support] и [approach].",
                        negativeText="но руины не имеют дела с чувствами. Ты выбегаешь в последний момент." },
                    new SynergyData { approach=WordArchetype.Physical, support=WordArchetype.Physical, bonusChance=-0.15f,
                        positiveText="и сила пробивает путь. Ты находишь [support].",
                        negativeText="но руины содрогаются. Ты слышишь трещины." }
                }
            },

            new EventData {
                key       = "Day5_Swamp",
                eventText = "Без подсказок ты забрёл в болото. Трясина чавкает под ногами, туман скрывает тропу.",
                penalty   = -18,
                actionVerb = "Пробраться", actionVerbPast = "пробираешься",
                defApproach = "терпеливо", defApproachPast = "терпеливо",
                defHp = 2, defPow = 1, defSan = 2,
                defSupport = "силой", defReduction = 0,
                positiveOutcome = "и ты находишь твёрдую почву. На кочке лежит [support] и [support].",
                negativeOutcome = "но болото затягивает глубже. Ты выбираешься с трудом.",
                rewardKeys = new[] { "strength", "luck", "cunning" },
                poolKeys   = new[] { "strength", "luck", "cunning", "patient" },
                weight    = 1f,
                dayMin = 5, dayMax = 5, isMandatory = true,
                baseChance = 0.25f,
                requiredFlags = new[] { "ignored_stranger" },
                setsFlagPos = "swamp_survived", setsFlagNeg = "swamp_lost",
                synergies = new[] {
                    new SynergyData { approach=WordArchetype.Physical, support=WordArchetype.Physical, bonusChance=0.2f,
                        positiveText="и ты пробиваешься через трясину. На кочке лежит [support] и [support].",
                        negativeText="но сила истощается в болоте. Ты выбираешься с трудом." },
                    new SynergyData { approach=WordArchetype.Mental, support=WordArchetype.Physical, bonusChance=0.15f,
                        positiveText="и тяжёлый камень как ориентир. На пути лежит [support] и [support].",
                        negativeText="но хитрость не знает обходных путей. Ты выбираешься с трудом." },
                    new SynergyData { approach=WordArchetype.Social, support=WordArchetype.Social, bonusChance=-0.15f,
                        positiveText="и чудом уходишь. Впереди — [support].",
                        negativeText="но слова не делают тропу. Болото затягивает глубже." }
                }
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
                positiveOutcome = "и знахарка благодарит: «Ты спас нас». Она вручает [support] и [approach].",
                negativeOutcome = "но жители кричат тебе вслед. Мор не отступает.",
                rewardKeys = new[] { "knowledge", "word", "patient" },
                poolKeys   = new[] { "knowledge", "word", "gold", "patient", "open" },
                weight    = 1f,
                dayMin = 6, dayMax = 6, isMandatory = true,
                baseChance = 0.3f,
                setsFlagPos = "helped_village", setsFlagNeg = "ignored_village",
                synergies = new[] {
                    new SynergyData { approach=WordArchetype.Mental, support=WordArchetype.Mental, bonusChance=0.25f,
                        positiveText="и знахарка удивлена: «Ты знаешь травы.» Она вручает [support] и [approach].",
                        negativeText="но знания не спасают целый мор. Жители кричат тебе вслед." },
                    new SynergyData { approach=WordArchetype.Social, support=WordArchetype.Social, bonusChance=0.15f,
                        positiveText="и деревня отвечает любовью: «Ты спас нас.» Ты находишь [support] и [approach].",
                        negativeText="но мор сильнее слов. Жители плачут за тобой." },
                    new SynergyData { approach=WordArchetype.Physical, support=WordArchetype.Physical, bonusChance=-0.15f,
                        positiveText="и сила иногда работает. Ты находишь [support].",
                        negativeText="но тела не исцелить руками. Мор не отступает." }
                }
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
                positiveOutcome = "и тролль хохочет, швыряя тебе [support] и [support]. Мост свободен.",
                negativeOutcome = "но тролль рычит и замахивается. Ты перебегаешь мост бегом.",
                rewardKeys = new[] { "cunning", "luck", "gold" },
                poolKeys   = new[] { "cunning", "luck", "gold", "secret" },
                weight    = 1f,
                dayMin = 7, dayMax = 7, isMandatory = true,
                baseChance = 0.25f,
                requiredFlags = new[] { "helped_village" },
                setsFlagPos = "troll_paid", setsFlagNeg = "troll_fight",
                synergies = new[] {
                    new SynergyData { approach=WordArchetype.Mental, support=WordArchetype.Social, bonusChance=0.25f,
                        positiveText="и тролль зачарован. «Хитро!» Он бросает тебе [support] и [support].",
                        negativeText="но тролль замечает обман. «Хитрый — но не хитрее меня.»" },
                    new SynergyData { approach=WordArchetype.Social, support=WordArchetype.Social, bonusChance=0.1f,
                        positiveText="и тролль задумывается. «Пусть идёт с миром.» Ты находишь [support] и [support].",
                        negativeText="но тролль скучает: «Слова меня не кормят.»" },
                    new SynergyData { approach=WordArchetype.Physical, support=WordArchetype.Physical, bonusChance=-0.15f,
                        positiveText="и тролль ошеломлён, пропускает. Ты находишь [support].",
                        negativeText="но тролль рычит и замахивается. Ты перебегаешь мост бегом." }
                }
            },

            new EventData {
                key       = "Day7_Gorge",
                eventText = "В узком ущелье тебя поджидают мародёры. Они знали, что ты пройдёшь здесь — кто-то донёс.",
                penalty   = -22,
                actionVerb = "Прорваться", actionVerbPast = "прорываешься",
                defApproach = "отчаянно", defApproachPast = "отчаянно",
                defHp = 2, defPow = 2, defSan = 1,
                defSupport = "силой", defReduction = 0,
                positiveOutcome = "и мародёры бегут, бросив награбленное. Среди вещей — [support] и [support].",
                negativeOutcome = "но мародёры сильнее. Ты вырываешься с потерями.",
                rewardKeys = new[] { "strength", "luck", "cunning" },
                poolKeys   = new[] { "strength", "luck", "cunning", "forceful", "desperate" },
                weight    = 1f,
                dayMin = 7, dayMax = 7, isMandatory = true,
                baseChance = 0.25f,
                requiredFlags = new[] { "ignored_village" },
                setsFlagPos = "gorge_escaped", setsFlagNeg = "gorge_wounded",
                synergies = new[] {
                    new SynergyData { approach=WordArchetype.Physical, support=WordArchetype.Physical, bonusChance=0.2f,
                        positiveText="и мародёры бегут. Среди вещей — [support] и [support].",
                        negativeText="но сила без тактики истощается. Ты вырываешься с потерями." },
                    new SynergyData { approach=WordArchetype.Mental, support=WordArchetype.Physical, bonusChance=0.15f,
                        positiveText="и обходной манёвр прорывается. Среди вещей — [support] и [support].",
                        negativeText="но хитрость не заменяет хорошей брони. Ты вырываешься с потерями." },
                    new SynergyData { approach=WordArchetype.Social, support=WordArchetype.Social, bonusChance=-0.15f,
                        positiveText="и слова на мгновение останавливают их. Ты находишь [support].",
                        negativeText="но шайка не слушает. Ты вырываешься, хотя и ранен." }
                }
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
                positiveOutcome = "и голем рассыпается. Среди обломков — [support] и [support].",
                negativeOutcome = "но голем наносит удар. Ты проскакиваешь, однако ранен.",
                rewardKeys = new[] { "strength", "cunning", "knowledge" },
                poolKeys   = new[] { "strength", "cunning", "knowledge", "forceful", "desperate" },
                weight    = 1f,
                dayMin = 8, dayMax = 8, isMandatory = true,
                baseChance = 0.2f,
                setsFlagPos = "golem_fallen", setsFlagNeg = "golem_wounded",
                synergies = new[] {
                    new SynergyData { approach=WordArchetype.Mental, support=WordArchetype.Mental, bonusChance=0.25f,
                        positiveText="и ты находишь руну управления. Голем замирает. Среди обломков — [support] и [support].",
                        negativeText="но рун управления нет. Голем наносит удар." },
                    new SynergyData { approach=WordArchetype.Physical, support=WordArchetype.Physical, bonusChance=0.15f,
                        positiveText="и ударов хватает. Голем рассыпается. Среди обломков — [support] и [support].",
                        negativeText="но камень не знает усталости. Голем наносит удар." },
                    new SynergyData { approach=WordArchetype.Social, support=WordArchetype.Social, bonusChance=-0.2f,
                        positiveText="и чудом голем останавливается. Ты находишь [support].",
                        negativeText="но конструкция не слышит слов. Голем наносит удар." }
                }
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
                positiveOutcome = "и портал схлопывается, выбросив [support] и [approach]. Тишина.",
                negativeOutcome = "но портал втягивает часть твоей силы и исчезает.",
                rewardKeys = new[] { "knowledge", "cunning", "patient" },
                poolKeys   = new[] { "knowledge", "cunning", "patient", "careful" },
                weight    = 1f,
                dayMin = 9, dayMax = 9, isMandatory = true,
                baseChance = 0.2f,
                requiredFlags = new[] { "spirits_gift" },
                setsFlagPos = "portal_crossed", setsFlagNeg = "portal_drained",
                synergies = new[] {
                    new SynergyData { approach=WordArchetype.Mental, support=WordArchetype.Mental, bonusChance=0.25f,
                        positiveText="и портал открывает путь. Ты читаешь его. Выброшено [support] и [approach].",
                        negativeText="но знаний о портале недостаточно. Он втягивает часть твоей силы." },
                    new SynergyData { approach=WordArchetype.Social, support=WordArchetype.Mental, bonusChance=0.1f,
                        positiveText="и портал отвечает на твою душу. Выброшено [support] и [approach].",
                        negativeText="но портал требует знаний, а не чувств. Он исчезает." },
                    new SynergyData { approach=WordArchetype.Physical, support=WordArchetype.Physical, bonusChance=-0.15f,
                        positiveText="и ты вырываешься силой. Выброшено [support].",
                        negativeText="но магия не уважает силу. Портал втягивает часть твоей силы." }
                }
            },

            new EventData {
                key       = "Day9_Voice",
                eventText = "Голос из-под земли обещает силу: «Дай мне что-нибудь... и я дам тебе гораздо больше.»",
                penalty   = -20,
                actionVerb = "Ответить", actionVerbPast = "отвечаешь",
                defApproach = "осторожно", defApproachPast = "осторожно",
                defHp = 1, defPow = 1, defSan = 3,
                defSupport = "словом", defReduction = 0,
                positiveOutcome = "и голос стихает. На земле — [support] и [approach]. Сделка заключена.",
                negativeOutcome = "но голос хохочет и замолкает. Земля дрожит под ногами.",
                rewardKeys = new[] { "cunning", "knowledge", "careful" },
                poolKeys   = new[] { "cunning", "knowledge", "careful", "patient" },
                weight    = 1f,
                dayMin = 9, dayMax = 9, isMandatory = true,
                baseChance = 0.2f,
                excludedFlags = new[] { "spirits_gift" },
                setsFlagPos = "voice_deal", setsFlagNeg = "voice_rejected",
                synergies = new[] {
                    new SynergyData { approach=WordArchetype.Social, support=WordArchetype.Social, bonusChance=0.2f,
                        positiveText="и голос доволен ответом. «Сделка.» На земле — [support] и [approach].",
                        negativeText="но голос смеётся: «Слова — дешёвый товар.» Земля дрожит." },
                    new SynergyData { approach=WordArchetype.Mental, support=WordArchetype.Social, bonusChance=0.1f,
                        positiveText="и ты осторожно торгуешься. Голос соглашается. На земле — [support] и [approach].",
                        negativeText="но голос видит твои расчёты. «Ты слишком умён для честной сделки.»" },
                    new SynergyData { approach=WordArchetype.Physical, support=WordArchetype.Physical, bonusChance=-0.2f,
                        positiveText="и голос испуган агрессией. Он оставляет [support] и замолкает.",
                        negativeText="но голос хохочет над силой: «Глупец!» Земля дрожит." }
                }
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
                positiveOutcome = "и свет пробивается сквозь тьму. Ты выжил. Путь окончен.",
                negativeOutcome = "но тьма поглощает тебя. Ты становишься частью дороги, которой шёл.",
                rewardKeys = Array.Empty<string>(),
                poolKeys   = new[] { "knowledge", "cunning", "strength" },
                weight    = 1f,
                dayMin = 10, dayMax = 10, isMandatory = true,
                baseChance = 0.15f,
                setsFlagPos = "survived_final", setsFlagNeg = "lost_final",
                synergies = new[] {
                    new SynergyData { approach=WordArchetype.Mental, support=WordArchetype.Mental, bonusChance=0.2f,
                        positiveText="и ты понимаешь. Всё сходится. Свет пробивается. Ты выжил.",
                        negativeText="но знания не меняют судьбы. Тьма поглощает тебя." },
                    new SynergyData { approach=WordArchetype.Physical, support=WordArchetype.Physical, bonusChance=0.15f,
                        positiveText="и воля не ломается. Ты прорываешься. Путь окончен.",
                        negativeText="но сил уже нет. Тьма поглощает тебя." },
                    new SynergyData { approach=WordArchetype.Social, support=WordArchetype.Social, bonusChance=0.1f,
                        positiveText="и прошлое не держит. Ты шагаешь вперёд. Путь окончен.",
                        negativeText="но слова не меняют конца. Тьма поглощает тебя." }
                }
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

                so.baseChance          = data.baseChance;
                so.positiveOutcomeText = data.positiveOutcome;
                so.negativeOutcomeText = data.negativeOutcome;

                so.synergies.Clear();
                if (data.synergies != null)
                    foreach (var sd in data.synergies)
                        so.synergies.Add(new SynergyEntry {
                            approachArchetype  = sd.approach,
                            supportArchetype   = sd.support,
                            bonusChance        = sd.bonusChance,
                            positiveOutcomeText = sd.positiveText ?? "",
                            negativeOutcomeText = sd.negativeText ?? ""
                        });

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
