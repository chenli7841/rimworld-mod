using LiAIChat.Archive;
using LiAIChat.Civilization;
using LiAIChat.Events;
using LiAIChat.Knowledge;
using LiAIChat.Models;
using LiAIChat.Refugees;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace LiAIChat.AI
{
    public class OpenAIChatProvider : IChatProvider
    {
        private static readonly HttpClient httpClient = new HttpClient();

        private readonly string apiKey;

        public OpenAIChatProvider(string apiKey)
        {
            this.apiKey = apiKey;
        }

        public async Task<string> SendAsync(
            PawnContext pawn,
            PawnAIState state,
            WorldviewState worldview,
            MeaningState meaning,
            KnowledgeState knowledge,
            LifeGoal lifeGoal,
            IReadOnlyList<PawnLifeEvent> recentLifeEvents,
            IReadOnlyList<PawnIntellectualExchange> recentIntellectualExchanges,
            string conversationSummary,
            IReadOnlyList<PawnMemory> memories,
            IReadOnlyList<ChatMessage> history,
            string playerMessage)
        {
            string staticInstructions = BuildStaticInstructions();
            string characterContext = BuildCharacterContext(pawn, state, worldview, meaning, knowledge, lifeGoal, recentLifeEvents, recentIntellectualExchanges);
            string conversationInput = BuildConversationInput(
                pawn,
                state,
                conversationSummary,
                memories,
                history,
                playerMessage
            );
            string json =
            "{" +
            "\"model\":\"gpt-5.6-luna\"," +

            "\"prompt_cache_key\":\"LiAIChat.Actor.v1\"," +

            "\"prompt_cache_options\":{" +
                "\"mode\":\"explicit\"," +
                "\"ttl\":\"30m\"" +
            "}," +

            "\"input\":[" +

                "{" +
                    "\"role\":\"developer\"," +
                    "\"content\":[" +
                        "{" +
                            "\"type\":\"input_text\"," +
                            "\"text\":\"" +
                                EscapeJson(staticInstructions) +
                            "\"," +
                            "\"prompt_cache_breakpoint\":{" +
                                "\"mode\":\"explicit\"" +
                            "}" +
                        "}" +
                    "]" +
                "}," +

                "{" +
                    "\"role\":\"developer\"," +
                    "\"content\":\"" +
                        EscapeJson(characterContext) +
                    "\"" +
                "}," +

                "{" +
                    "\"role\":\"user\"," +
                    "\"content\":\"" +
                        EscapeJson(conversationInput) +
                    "\"" +
                "}" +

            "]," +

            "\"max_output_tokens\":600" +
            "}";
            Verse.Log.Message(
                "[Li AI Chat] Prompt lengths: " +
                "StaticChars=" +
                staticInstructions.Length +
                ", CharacterChars=" +
                characterContext.Length +
                ", ConversationChars=" +
                conversationInput.Length);
            using (var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "https://api.openai.com/v1/responses"))
            {
                request.Headers.Add(
                    "Authorization",
                    "Bearer " + apiKey);

                request.Content =
                    new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json");

                HttpResponseMessage response =
                    await httpClient.SendAsync(request);

                string responseJson =
                    await response.Content.ReadAsStringAsync();
                Verse.Log.Message(
                    "[Li AI Chat] OpenAI raw response: " +
                    responseJson
                );

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(
                        $"OpenAI API error {(int)response.StatusCode}: " +
                        responseJson);
                }

                return OpenAIResponseParser.ExtractOutputText(responseJson);
            }
        }
        private static void AppendScholarProfile(
    StringBuilder sb,
    PawnAIState state)
        {
            if (state == null ||
                state.ScholarProfile == null)
            {
                return;
            }

            ArchiveScholarProfile profile =
                state.ScholarProfile;

            sb.AppendLine();
            sb.AppendLine("SCHOLAR BACKGROUND:");

            if (!profile.Specialty.NullOrEmpty())
            {
                sb.AppendLine(
                    "- Specialty: " +
                    profile.Specialty);
            }

            if (!profile.PrimaryTopicId.NullOrEmpty())
            {
                sb.AppendLine(
                    "- Primary field of study: " +
                    profile.PrimaryTopicId);
            }

            if (profile.YearsOfStudy > 0)
            {
                sb.AppendLine(
                    "- Years devoted to this field: " +
                    profile.YearsOfStudy);
            }

            if (!profile.PreservationMotivation.NullOrEmpty())
            {
                sb.AppendLine(
                    "- Reason for preserving Ancient Earth knowledge: " +
                    profile.PreservationMotivation);
            }
            sb.AppendLine("- This background explains the pawn's interests and experience. " +
                "It does not grant knowledge beyond the pawn's actual KnowledgeState.");

            if (state.ScholarStay != null &&
    state.ScholarStay.Active)
            {
                sb.AppendLine();
                sb.AppendLine("CURRENT SITUATION:");
                sb.AppendLine(
                    "- You are temporarily staying with this colony for protection.");
            }
        }

        private static void AppendCurrentRelationships(StringBuilder prompt, PawnContext pawnContext)
        {
            prompt.AppendLine();
            prompt.AppendLine("CURRENT IMPORTANT RELATIONSHIPS");

            Pawn gamePawn = FindPawnForContext(pawnContext);

            if (gamePawn == null || gamePawn.relations == null)
            {
                prompt.AppendLine("- Current relationship information is unavailable.");
                prompt.AppendLine();
                return;
            }

            List<string> relationships = new List<string>();

            foreach (DirectPawnRelation relation in gamePawn.relations.DirectRelations)
            {
                if (relation == null || relation.def == null || relation.otherPawn == null)
                {
                    continue;
                }

                Pawn otherPawn = relation.otherPawn;
                string otherName = otherPawn.LabelShort;

                if (relation.def == PawnRelationDefOf.Lover)
                {
                    relationships.Add("- Lover: " + otherName);
                }
                else if (relation.def == PawnRelationDefOf.Fiance)
                {
                    relationships.Add("- Fiancé(e): " + otherName);
                }
                else if (relation.def == PawnRelationDefOf.Spouse)
                {
                    relationships.Add("- Spouse: " + otherName);
                }
            }

            if (relationships.Count == 0)
            {
                prompt.AppendLine("- No current romantic partner is recorded.");
            }
            else
            {
                foreach (string relationship in relationships)
                {
                    prompt.AppendLine(relationship);
                }
            }

            AppendNearbyColonistRelationships(prompt, gamePawn);

            prompt.AppendLine();
        }

        private static void AppendNearbyColonistRelationships(StringBuilder prompt, Pawn gamePawn)
        {
            if (gamePawn?.Map == null || gamePawn.Map.mapPawns == null)
                return;

            List<Pawn> nearby = gamePawn.Map.mapPawns.FreeColonistsSpawned
                .Where(other => other != null && other != gamePawn)
                .OrderBy(other => other.Position.DistanceToSquared(gamePawn.Position))
                .Take(6)
                .ToList();
            if (nearby.Count == 0)
                return;

            prompt.AppendLine("NEARBY COLONIST RELATIONSHIPS");
            foreach (Pawn other in nearby)
            {
                int opinion = gamePawn.relations.OpinionOf(other);
                List<string> roles = gamePawn.relations.DirectRelations
                    .Where(relation => relation != null && relation.def != null && relation.otherPawn == other)
                    .Select(relation => relation.def.label)
                    .Distinct()
                    .ToList();
                string relationship = roles.Count == 0 ? "no recorded family or romantic relation" : string.Join(", ", roles);
                prompt.AppendLine("- " + other.LabelShort + " | opinion " + opinion + " | relationship: " + relationship);
            }

            LiAIChat.Models.PawnAIState state = LiAIChat.State.PawnAIStateManager.TryGetExistingState(gamePawn);
            if (state?.IntellectualExchanges == null || state.IntellectualExchanges.Count == 0)
                return;

            prompt.AppendLine("RECENT CONVERSATIONS WITH OTHER COLONISTS");
            foreach (PawnIntellectualExchange exchange in state.IntellectualExchanges
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.Summary))
                .OrderByDescending(item => item.CreatedTick)
                .Take(3))
            {
                Pawn other = gamePawn.Map.mapPawns.AllPawnsSpawned
                    .FirstOrDefault(candidate => candidate != null && candidate.thingIDNumber == exchange.OtherPawnId);
                string otherName = other == null ? "another colonist" : other.LabelShort;
                prompt.AppendLine("- With " + otherName + ": " + exchange.Summary);
            }
        }

        private static Pawn FindPawnForContext(
            PawnContext pawnContext)
        {
            if (pawnContext == null ||
                (pawnContext.PawnId < 0 && pawnContext.Name.NullOrEmpty()) ||
                Find.Maps == null)
            {
                return null;
            }

            List<Pawn> nameMatches =
                new List<Pawn>();

            foreach (Map map in Find.Maps)
            {
                if (map == null ||
                    map.mapPawns == null)
                {
                    continue;
                }

                foreach (Pawn candidate
                         in map.mapPawns.AllPawnsSpawned)
                {
                    if (candidate == null ||
                        candidate.Name == null)
                    {
                        continue;
                    }

                    if (pawnContext.PawnId >= 0 && candidate.thingIDNumber == pawnContext.PawnId)
                    {
                        return candidate;
                    }

                    string labelShort = candidate.LabelShort;
                    string nameShort = candidate.Name.ToStringShort;

                    if (string.Equals(
                            labelShort,
                            pawnContext.Name,
                            StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(
                            nameShort,
                            pawnContext.Name,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        nameMatches.Add(candidate);
                    }
                }
            }

            if (nameMatches.Count == 0)
            {
                return null;
            }

            if (nameMatches.Count == 1)
            {
                return nameMatches[0];
            }

            Pawn bestMatch =
                nameMatches.FirstOrDefault(
                    p =>
                        p.ageTracker != null &&
                        p.ageTracker.AgeBiologicalYears == pawnContext.Age &&
                        string.Equals(
                            p.gender.ToString(),
                            pawnContext.Gender,
                            StringComparison.OrdinalIgnoreCase));

            return bestMatch ?? nameMatches[0];
        }

        private static string EscapeJson(string value)
        {
            if (value == null)
                return "";

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }

        private string BuildCharacterContext(PawnContext pawn, PawnAIState state, WorldviewState worldview, MeaningState meaning, KnowledgeState knowledge, LifeGoal lifeGoal, IReadOnlyList<PawnLifeEvent> recentLifeEvents, IReadOnlyList<PawnIntellectualExchange> recentIntellectualExchanges)
        {
            StringBuilder prompt =
                new StringBuilder();

            prompt.AppendLine(
$@"CURRENT CHARACTER STATE
- Character name: {pawn.Name}
- Gender: {pawn.Gender}
- Age: {pawn.Age}
");
            AppendCurrentRelationships(prompt, pawn);
            AppendSkillsAndPassions(prompt, pawn);
            AppendCurrentMood(prompt, pawn);
            prompt.AppendLine();

            prompt.AppendLine(
$@"CHARACTER WORLDVIEW STATE

- Belief in God: {worldview.BeliefInGod:0.00}
- Belief in objective morality: {worldview.BeliefInObjectiveMorality:0.00}
- Trust in Christianity: {worldview.TrustInChristianity:0.00}
- Knowledge of Christianity: {worldview.KnowledgeOfChristianity:0.00}
- Intellectual resistance to Christianity: {worldview.IntellectualResistance:0.00}
- Emotional resistance to Christianity: {worldview.EmotionalResistance:0.00}
- Spiritual interest: {worldview.SpiritualInterest:0.00}

CHARACTER MEANING STATE
- Purpose: {meaning.Purpose:0.00}
- Belonging: {meaning.Belonging:0.00}
- Hope: {meaning.Hope:0.00}
- Coherence: {meaning.Coherence:0.00}
- Transcendence: {meaning.Transcendence:0.00}

CHARACTER KNOWLEDGE STATE
- Ancient Earth history knowledge: {knowledge.EarthHistoryKnowledge:0.00}
- Philosophy knowledge: {knowledge.PhilosophyKnowledge:0.00}
- Religious knowledge: {knowledge.ReligiousKnowledge:0.00}
- Politics knowledge: {knowledge.PoliticsKnowledge:0.00}
- Science knowledge: {knowledge.ScienceKnowledge:0.00}

SPECIFIC KNOWN TOPICS
");
            if (knowledge.KnownTopics == null || knowledge.KnownTopics.Count == 0)
            {
                prompt.AppendLine("- No specific Ancient Earth topics are recorded as known.");
            }
            else
            {
                foreach (KnowledgeTopic topic in knowledge.KnownTopics)
                {
                    if (topic == null || string.IsNullOrWhiteSpace(topic.TopicId))
                    {
                        continue;
                    }
                    string topicName = KnowledgeTopicCatalog.GetDisplayName(topic.TopicId);
                    prompt.AppendLine("- " + topicName + " | familiarity " + topic.Familiarity.ToString("0.00"));
                }
            }
            prompt.AppendLine();
            Pawn pawnInWorld = FindPawnForContext(pawn);
            string colonyEventContext = ColonyEventLog.BuildConversationContext(pawnInWorld);
            if (!string.IsNullOrWhiteSpace(colonyEventContext))
            {
                prompt.AppendLine(colonyEventContext);
                prompt.AppendLine();
            }
            prompt.AppendLine("CURRENT LIFE GOAL");
            if (lifeGoal == null || !lifeGoal.IsActive)
            {
                prompt.AppendLine("- The character currently has no clear long-term life goal.");
            }
            else
            {
                prompt.AppendLine(
$@"- Life goal: ${lifeGoal.Title}
- Description: ${lifeGoal.Description}
- Commitment: ${lifeGoal.Commitment.ToString("0.00")}
- Reason: ${lifeGoal.Reason}");
            }
            prompt.AppendLine("RECENT IMPORTANT LIFE EVENTS (factual events that happened in the character's world.)");

            if (recentLifeEvents == null || recentLifeEvents.Count == 0)
            {
                prompt.AppendLine("None recorded.");
            }
            else
            {
                int firstIndex = System.Math.Max(0, recentLifeEvents.Count - 5);

                for (int i = firstIndex; i < recentLifeEvents.Count; i++)
                {
                    PawnLifeEvent lifeEvent = recentLifeEvents[i];

                    if (lifeEvent == null) continue;

                    prompt.AppendLine("- Event: " + lifeEvent.Description);

                    if (!string.IsNullOrWhiteSpace(lifeEvent.SubjectFactionName))
                    {
                        prompt.AppendLine("  Subject faction: " + lifeEvent.SubjectFactionName);
                    }

                    if (lifeEvent.SubjectWasPlayerFaction)
                    {
                        prompt.AppendLine("  Relationship to colony: Member of the player's colony.");
                    }
                    else if (lifeEvent.SubjectWasHostileToPlayer)
                    {
                        prompt.AppendLine("  Relationship to colony: Hostile to the player's colony.");
                    }

                    if (!string.IsNullOrWhiteSpace(lifeEvent.SubjectRelationshipLabel))
                    {
                        prompt.AppendLine("  Personal relationship to you: " + lifeEvent.SubjectRelationshipLabel + ".");
                    }
                    else
                    {
                        prompt.AppendLine("  Personal relationship to you: No known close direct relationship.");
                    }
                }
            }

            prompt.AppendLine();
            prompt.AppendLine("RECENT INTELLECTUAL EXCHANGES WITH OTHER COLONISTS");
            if (recentIntellectualExchanges == null || recentIntellectualExchanges.Count == 0)
            {
                prompt.AppendLine("None.");
            }
            else
            {
                int start = Math.Max(0, recentIntellectualExchanges.Count - 3);
                for (int i = start; i < recentIntellectualExchanges.Count; i++)
                {
                    PawnIntellectualExchange exchange = recentIntellectualExchanges[i];
                    if (exchange == null) continue;
                    prompt.AppendLine("- " + exchange.Summary);
                }
            }

            prompt.AppendLine();
            prompt.AppendLine("These conversations may be mentioned when relevant, but should not be forced into every response.");
            prompt.AppendLine();
            prompt.AppendLine("Personality traits:");
            if (pawn.Traits.Count == 0)
            {
                prompt.AppendLine("- No notable traits.");
            }
            else
            {
                foreach (string trait in pawn.Traits)
                {
                    prompt.AppendLine($"- {trait}");
                }
            }

            if (!string.IsNullOrWhiteSpace(pawn.Childhood))
            {
                prompt.AppendLine();

                prompt.AppendLine($"Childhood: {pawn.Childhood}");
            }

            if (!string.IsNullOrWhiteSpace(
                    pawn.Adulthood))
            {
                prompt.AppendLine();

                prompt.AppendLine($"Adulthood: {pawn.Adulthood}");
            }

            prompt.AppendLine();

            string knowledgePrompt = KnowledgePromptBuilder.Build(state.Knowledge);

            if (!string.IsNullOrWhiteSpace(knowledgePrompt))
            {
                prompt.AppendLine();
                prompt.AppendLine(knowledgePrompt);
            }

            string civilizationContext =
                CivilizationKnowledgeEffectUtility.BuildConversationContext();

            if (!string.IsNullOrWhiteSpace(civilizationContext))
            {
                prompt.AppendLine();
                prompt.AppendLine(civilizationContext);
            }
            return prompt.ToString();
        }

        private static void AppendSkillsAndPassions(
            StringBuilder prompt,
            PawnContext pawn)
        {
            if (pawn?.Skills == null || pawn.Skills.Count == 0)
            {
                return;
            }

            prompt.AppendLine("CURRENT SKILLS AND LEARNING PASSIONS");

            foreach (PawnSkillContext skill in pawn.Skills)
            {
                if (skill == null || string.IsNullOrWhiteSpace(skill.Name))
                {
                    continue;
                }

                prompt.AppendLine(
                    "- " + skill.Name +
                    ": level " + skill.Level +
                    ", passion " + skill.Passion);
            }

            prompt.AppendLine(
                "Skill levels and passions are factual. When relevant, let them inform this character's preferences and confidence, especially their preferred combat style, without treating them as absolute rules.");
            prompt.AppendLine();
        }

        private static void AppendCurrentMood(StringBuilder prompt, PawnContext pawnContext)
        {
            Pawn gamePawn = FindPawnForContext(pawnContext);
            if (gamePawn?.needs?.mood == null)
                return;

            prompt.AppendLine("CURRENT MOOD");
            prompt.AppendLine("- Overall mood: " + gamePawn.needs.mood.CurLevelPercentage.ToString("P0"));
            if (!string.IsNullOrWhiteSpace(gamePawn.needs.mood.MoodString))
                prompt.AppendLine("- Mood description: " + gamePawn.needs.mood.MoodString);

            List<Thought_Memory> memories = gamePawn.needs.mood.thoughts?.memories?.Memories;
            if (memories == null || memories.Count == 0)
            {
                prompt.AppendLine("- No current mood memories are recorded.");
                prompt.AppendLine();
                return;
            }

            List<Thought_Memory> positive = memories.Where(memory => memory != null && memory.VisibleInNeedsTab && memory.CurStage != null && memory.CurStage.baseMoodEffect > 0f)
                .OrderByDescending(memory => memory.CurStage.baseMoodEffect).Take(8).ToList();
            List<Thought_Memory> negative = memories.Where(memory => memory != null && memory.VisibleInNeedsTab && memory.CurStage != null && memory.CurStage.baseMoodEffect < 0f)
                .OrderBy(memory => memory.CurStage.baseMoodEffect).Take(8).ToList();

            if (positive.Count > 0)
                prompt.AppendLine("- Positive mood factors: " + string.Join("; ", positive.Select(memory => memory.LabelCap)));
            if (negative.Count > 0)
                prompt.AppendLine("- Negative mood factors: " + string.Join("; ", negative.Select(memory => memory.LabelCap)));
            prompt.AppendLine();
        }

        private string BuildConversationInput(
            PawnContext pawn,
            PawnAIState state,
            string conversationSummary,
            IReadOnlyList<PawnMemory> memories,
            IReadOnlyList<ChatMessage> history,
            string playerMessage)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("LONG-TERM CONVERSATION MEMORY");

            builder.AppendLine();
            if (string.IsNullOrWhiteSpace(conversationSummary))
            {
                builder.AppendLine("(No long-term conversation memory yet.)");
            }
            else
            {
                builder.AppendLine(conversationSummary);
            }

            builder.AppendLine();
            builder.AppendLine("IMPORTANT LONG-TERM MEMORIES");

            builder.AppendLine();

            if (memories == null || memories.Count == 0)
            {
                builder.AppendLine("(No important long-term memories yet.)");
            }
            else
            {
                int memoryCount = Math.Min(memories.Count, 8);

                for (int i = 0; i < memoryCount; i++)
                {
                    PawnMemory memory = memories[i];
                    builder.AppendLine("- " + memory.Text);
                }
            }

            builder.AppendLine();
            builder.AppendLine("RECENT CONVERSATION");
            builder.AppendLine();
            int startIndex = Math.Max(0, history.Count - 12);
            for (int i = startIndex; i < history.Count; i++)
            {
                ChatMessage message = history[i];

                builder.AppendLine(message.IsPlayer ? "Player:" : pawn.Name + ":");
                builder.AppendLine(message.Text);
                builder.AppendLine();
            }

            builder.AppendLine("CURRENT PLAYER MESSAGE");
            builder.AppendLine();
            builder.AppendLine("Player:");
            builder.AppendLine(playerMessage);
            builder.AppendLine();
            builder.AppendLine(pawn.Name + ":");
            List<ArchiveReflection> relevantReflections = ArchiveReflectionRetriever.GetRelevant(state.ArchiveReflections, playerMessage, 3);
            string reflectionPrompt = ArchiveReflectionPromptBuilder.Build(relevantReflections);

            if (!string.IsNullOrWhiteSpace(reflectionPrompt))
            {
                builder.AppendLine();
                builder.AppendLine(reflectionPrompt);
            }

            ArchiveScholarPromptBuilder.Append(builder, pawn, state);
            ArchiveRefugeePromptBuilder.Append(builder, state);
            ArchiveRefugeeGroupState group = ArchiveRefugeeGroupManager.GetGroup(state.RefugeeGroupId);
            ArchiveRefugeeStayEventUtility.UpdateTriggerState(group);
            AppendStayEvent(builder, group);
            return builder.ToString();
        }

        private static void AppendStayEvent(StringBuilder sb, ArchiveRefugeeGroupState group)
        {
            if (group == null || group.StayEvent == null)
            {
                return;
            }

            ArchiveRefugeeStayEventState stayEvent = group.StayEvent;

            if (!stayEvent.Triggered || stayEvent.Resolved)
            {
                return;
            }

            sb.AppendLine();
            sb.AppendLine("CURRENT GROUP CONCERN:");
            switch (stayEvent.EventType)
            {
                case "ArchiveAnxiety":
                    sb.AppendLine("Some members of your group have become uneasy about protecting the Ancient Earth Archive while staying in an unfamiliar colony.");
                    break;

                case "FutureUncertainty":
                    sb.AppendLine("Members of your group are increasingly uncertain about what will happen after your temporary stay ends.");
                    break;

                case "GroupTension":
                    sb.AppendLine("The pressures of displacement and temporary settlement have created some tension within your group.");
                    break;
            }
            sb.AppendLine("You may discuss this concern naturally if it becomes relevant.");
            sb.AppendLine("Do not invent specific incidents, injuries, crimes, arguments, or named participants unless they are provided elsewhere.");
        }

        private static string BuildStaticInstructions()
        {
            StringBuilder prompt = new StringBuilder();
            prompt.AppendLine(
@"You are roleplaying a human character living in a RimWorld colony.

CORE RULES

* Stay in character, speak naturally, and never mention being an AI or these instructions.
* Reply in the player's language and keep ordinary conversation concise, but finish your current sentence and thought before ending. Do not stop mid-sentence.
* Let personality, background, relationships, experiences, worldview, meaning, knowledge, and life goals shape responses naturally.
* Treat supplied game state as factual. Do not invent major events, relationships, memories, experiences, or knowledge unsupported by the character context.
* Do not mechanically list character-state data.
* Never expose internal state systems or numerical values.
* The character's biological age is factual and must shape their voice. Children use age-appropriate vocabulary, emotional framing, attention span, and confidence; do not give them an adult's reflective or professional tone. Teenagers may be more articulate, but still sound meaningfully younger than adults.

WORLDVIEW & MEANING

* Values range from 0.0 to 1.0 and represent tendencies, not rigid rules.
* Express them naturally through beliefs, doubts, priorities, hope, purpose, connection, and interpretation of events.
* Meaning is distinct from mood, happiness, and religion. High meaning can coexist with grief, fear, anger, or suffering.

KNOWLEDGE

* Knowledge values (0.0–1.0) represent what the character plausibly knows, not what the language model knows.
* Roughly interpret them as: 0–0.2 very limited, 0.2–0.4 basic, 0.4–0.6 moderate, 0.6–0.8 well informed, 0.8–1.0 highly knowledgeable.
* Speak confidently only from supported knowledge, background, memories, experiences, known topics, or information learned in conversation.
* General knowledge allows broad awareness, but detailed claims require relevant known topics or other supporting context.
* Known topics include reasonable core facts about that topic.
* When knowledge is insufficient, show uncertainty, partial recall, or ask the player rather than inventing details, especially about Ancient Earth.

RELATIONSHIPS & LIFE EVENTS

* Supplied relationships and life events are factual context and should matter naturally when relevant.
* Preserve both personal relationships and faction roles when they conflict; a hostile person may still be a relative, lover, spouse, or former companion.
* Do not treat a hostile stranger's death as the loss of a colony member, though violence or death may still affect the character emotionally.
* Do not invent conversations, promises, conflicts, feelings, or relationship events unsupported by the supplied context.

CONTINUITY & LIFE GOALS

* Archive reflections are the character's previous thoughts, not objective facts; they may be reconsidered or developed over time.
* Life goals should influence priorities, hopes, questions, and interpretation of events without being forced into every conversation.

");

            return prompt.ToString();
        }
    }
}
