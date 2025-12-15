using Newtonsoft.Json.Linq;
using NJsonSchema;
using System.Linq;
using System.Text.Json.Nodes;

namespace Consist.GPTDataExtruction.Extensions
{
    public static class SchemaFixerExtensions
    {
        public static JsonNode ToOpenAISchemaNode<T>(this JsonSchema _)
        {
            // Generate raw schema
            var raw = JsonSchema.FromType<T>();
            JObject root = JObject.Parse(raw.ToJson());

            // Force OpenAI-compatible root
            root["title"] = typeof(T).Name;
            root["type"] = "object";

            // ----------------------------------------------------------
            // ORDER IS CRITICAL
            // ----------------------------------------------------------

            // 1. Remove $schema
            RemoveDollarSchema(root);

            // 2. Remove format
            RemoveFormatRecursively(root);

            // 3. Remove allOf (OpenAI does NOT support it)
            RemoveAllOf(root);

            // 4. Flatten $ref
            if (root["definitions"] is JObject defs)
                FlattenAllRefs(root, defs);

            root.Remove("definitions");

            // 5. Normalize nullable unions
            NormalizeNullableTypes(root);

            // 6. Remove oneOf
            RemoveOneOf(root);

            // 7. Convert integer → number
            FixIntegerTypes(root);

            // 8. Add required[] safely
            AddRequiredRecursive(root);

            // 9. Enforce additionalProperties:false
            EnforceAdditionalPropertiesFalse(root);

            // 10. Final integer pass
            FixIntegerTypes(root);

            return JsonNode.Parse(root.ToString());
        }

        // ----------------------------------------------------------
        // Remove $schema
        // ----------------------------------------------------------
        private static void RemoveDollarSchema(JToken token)
        {
            if (token is JObject obj)
            {
                obj.Remove("$schema");
                foreach (var p in obj.Properties())
                    RemoveDollarSchema(p.Value);
            }
            else if (token is JArray arr)
            {
                foreach (var c in arr)
                    RemoveDollarSchema(c);
            }
        }

        // ----------------------------------------------------------
        // Remove format
        // ----------------------------------------------------------
        private static void RemoveFormatRecursively(JToken token)
        {
            if (token is JObject obj)
            {
                obj.Remove("format");
                foreach (var p in obj.Properties())
                    RemoveFormatRecursively(p.Value);
            }
            else if (token is JArray arr)
            {
                foreach (var c in arr)
                    RemoveFormatRecursively(c);
            }
        }

        // ----------------------------------------------------------
        // Remove allOf (MANDATORY for OpenAI)
        // ----------------------------------------------------------
        private static void RemoveAllOf(JToken token)
        {
            if (token is JObject obj)
            {
                if (obj["allOf"] is JArray allOf)
                {
                    obj.Remove("allOf");

                    foreach (var entry in allOf.OfType<JObject>())
                    {
                        foreach (var prop in entry.Properties())
                        {
                            if (obj[prop.Name] == null)
                                obj[prop.Name] = prop.Value.DeepClone();
                        }
                    }
                }

                foreach (var p in obj.Properties())
                    RemoveAllOf(p.Value);
            }
            else if (token is JArray arr)
            {
                foreach (var c in arr)
                    RemoveAllOf(c);
            }
        }

        // ----------------------------------------------------------
        // Flatten $ref
        // ----------------------------------------------------------
        private static void FlattenAllRefs(JToken token, JObject definitions)
        {
            if (token is JObject obj)
            {
                if (obj.TryGetValue("$ref", out var refToken))
                {
                    string refName = refToken.ToString().Replace("#/definitions/", "");
                    obj.Remove("$ref");

                    if (definitions.TryGetValue(refName, out var def))
                    {
                        foreach (var p in ((JObject)def).Properties())
                            obj[p.Name] = p.Value.DeepClone();
                    }
                }

                foreach (var p in obj.Properties())
                    FlattenAllRefs(p.Value, definitions);
            }
            else if (token is JArray arr)
            {
                foreach (var c in arr)
                    FlattenAllRefs(c, definitions);
            }
        }

        // ----------------------------------------------------------
        // Remove oneOf
        // ----------------------------------------------------------
        private static void RemoveOneOf(JToken token)
        {
            if (token is JObject obj)
            {
                if (obj["oneOf"] is JArray arr)
                {
                    obj.Remove("oneOf");

                    var chosen = arr
                        .OfType<JObject>()
                        .FirstOrDefault(o => o["properties"] != null || o["type"]?.ToString() == "object");

                    if (chosen != null)
                    {
                        foreach (var p in chosen.Properties())
                            obj[p.Name] = p.Value.DeepClone();
                    }
                }

                foreach (var p in obj.Properties())
                    RemoveOneOf(p.Value);
            }
            else if (token is JArray a)
            {
                foreach (var c in a)
                    RemoveOneOf(c);
            }
        }

        // ----------------------------------------------------------
        // Normalize nullable unions
        // ----------------------------------------------------------
        private static void NormalizeNullableTypes(JToken token)
        {
            if (token is JObject obj)
            {
                if (obj["type"] is JArray arr)
                {
                    obj["type"] = arr
                        .Select(v => v.ToString())
                        .FirstOrDefault(v => v != "null") ?? "string";
                }

                foreach (var p in obj.Properties())
                    NormalizeNullableTypes(p.Value);
            }
            else if (token is JArray a)
            {
                foreach (var c in a)
                    NormalizeNullableTypes(c);
            }
        }

        // ----------------------------------------------------------
        // Convert integer → number
        // ----------------------------------------------------------
        private static void FixIntegerTypes(JToken token)
        {
            if (token is JObject obj)
            {
                if (obj["type"]?.ToString() == "integer")
                    obj["type"] = "number";

                foreach (var p in obj.Properties())
                    FixIntegerTypes(p.Value);
            }
            else if (token is JArray arr)
            {
                foreach (var c in arr)
                    FixIntegerTypes(c);
            }
        }

        // ----------------------------------------------------------
        // Add required[] safely
        // ----------------------------------------------------------
        private static void AddRequiredRecursive(JToken token)
        {
            if (token is JObject obj)
            {
                if (obj["type"]?.ToString() == "object" &&
                    obj["properties"] is JObject props &&
                    props.Properties().Any())
                {
                    obj["required"] = new JArray(props.Properties().Select(p => p.Name));
                }

                foreach (var p in obj.Properties())
                    AddRequiredRecursive(p.Value);
            }
            else if (token is JArray arr)
            {
                foreach (var c in arr)
                    AddRequiredRecursive(c);
            }
        }

        // ----------------------------------------------------------
        // Enforce additionalProperties:false
        // ----------------------------------------------------------
        private static void EnforceAdditionalPropertiesFalse(JToken token)
        {
            if (token is JObject obj)
            {
                if (obj["type"]?.ToString() == "object")
                {
                    obj["additionalProperties"] ??= false;
                    obj["properties"] ??= new JObject();
                    obj["required"] ??= new JArray();
                }

                foreach (var p in obj.Properties())
                    EnforceAdditionalPropertiesFalse(p.Value);
            }
            else if (token is JArray arr)
            {
                foreach (var c in arr)
                    EnforceAdditionalPropertiesFalse(c);
            }
        }
    }
}
