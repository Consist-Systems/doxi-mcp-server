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
            // Step 1 — Generate raw schema
            var raw = JsonSchema.FromType<T>();
            JObject root = JObject.Parse(raw.ToJson());

            // Step 2 — Force valid OpenAI root fields
            root["title"] = typeof(T).Name;
            root["type"] = "object";

            // ----------------------------------------------------------
            // 🔥 ORDER MATTERS — DO NOT MOVE THESE STEPS
            // ----------------------------------------------------------

            // 1. Remove $schema
            RemoveDollarSchema(root);

            // 2. Remove format fields
            RemoveFormatRecursively(root);

            // 3. Flatten $ref references
            if (root["definitions"] is JObject defs)
                FlattenAllRefs(root, defs);

            root.Remove("definitions");

            // 4. Normalize nullable types: ["string","null"] → "string"
            NormalizeNullableTypes(root);

            // 5. Remove oneOf constructs
            RemoveOneOf(root);

            // 6. FIRST PASS: Convert "integer" → "number"
            FixIntegerTypes(root);

            // 7. Add required[] to every object
            AddRequiredRecursive(root);

            // 8. Enforce additionalProperties:false on all objects
            EnforceAdditionalPropertiesFalse(root);

            // 9. FINAL PASS: Convert integer → number AGAIN
            FixIntegerTypes(root);

            // Return clean JSON schema
            return JsonNode.Parse(root.ToString());
        }

        // ----------------------------------------------------------
        // REMOVE $schema
        // ----------------------------------------------------------
        private static void RemoveDollarSchema(JToken token)
        {
            if (token.Type == JTokenType.Object)
            {
                var obj = (JObject)token;

                if (obj.Property("$schema") != null)
                    obj.Remove("$schema");

                foreach (var p in obj.Properties())
                    RemoveDollarSchema(p.Value);
            }
            else if (token.Type == JTokenType.Array)
            {
                foreach (var child in token.Children())
                    RemoveDollarSchema(child);
            }
        }

        // ----------------------------------------------------------
        // REMOVE "format"
        // ----------------------------------------------------------
        private static void RemoveFormatRecursively(JToken token)
        {
            if (token.Type == JTokenType.Object)
            {
                var obj = (JObject)token;

                if (obj.Property("format") != null)
                    obj.Remove("format");

                foreach (var p in obj.Properties())
                    RemoveFormatRecursively(p.Value);
            }
            else if (token.Type == JTokenType.Array)
            {
                foreach (var child in token.Children())
                    RemoveFormatRecursively(child);
            }
        }

        // ----------------------------------------------------------
        // Convert type "integer" → "number"
        // ----------------------------------------------------------
        private static void FixIntegerTypes(JToken token)
        {
            if (token.Type == JTokenType.Object)
            {
                var obj = (JObject)token;

                if (obj["type"]?.ToString() == "integer")
                    obj["type"] = "number";

                foreach (var p in obj.Properties())
                    FixIntegerTypes(p.Value);
            }
            else if (token.Type == JTokenType.Array)
            {
                foreach (var child in token.Children())
                    FixIntegerTypes(child);
            }
        }

        // ----------------------------------------------------------
        // Flatten all $ref references
        // ----------------------------------------------------------
        private static void FlattenAllRefs(JToken token, JObject definitions)
        {
            if (token.Type == JTokenType.Object)
            {
                var obj = (JObject)token;

                if (obj.TryGetValue("$ref", out var refToken))
                {
                    string refPath = refToken.ToString().Replace("#/definitions/", "");
                    obj.Remove("$ref");

                    if (definitions.TryGetValue(refPath, out var def))
                    {
                        foreach (var prop in ((JObject)def).Properties())
                        {
                            obj[prop.Name] = prop.Value.DeepClone();
                        }
                    }
                }

                foreach (var p in obj.Properties())
                    FlattenAllRefs(p.Value, definitions);
            }
            else if (token.Type == JTokenType.Array)
            {
                foreach (var child in token.Children())
                    FlattenAllRefs(child, definitions);
            }
        }

        // ----------------------------------------------------------
        // Remove oneOf blocks
        // ----------------------------------------------------------
        private static void RemoveOneOf(JToken token)
        {
            if (token.Type == JTokenType.Object)
            {
                var obj = (JObject)token;

                if (obj["oneOf"] is JArray arr)
                {
                    var concrete = arr
                        .OfType<JObject>()
                        .FirstOrDefault(o =>
                            o["type"]?.ToString() == "object" ||
                            o["properties"] is JObject);

                    obj.Remove("oneOf");

                    if (concrete != null)
                    {
                        foreach (var prop in concrete.Properties())
                            obj[prop.Name] = prop.Value.DeepClone();
                    }
                    else
                    {
                        obj["type"] = "object";
                    }
                }

                foreach (var p in obj.Properties())
                    RemoveOneOf(p.Value);
            }
            else if (token.Type == JTokenType.Array)
            {
                foreach (var child in token.Children())
                    RemoveOneOf(child);
            }
        }

        // ----------------------------------------------------------
        // Fix nullable union types
        // ----------------------------------------------------------
        private static void NormalizeNullableTypes(JToken token)
        {
            if (token.Type == JTokenType.Object)
            {
                var obj = (JObject)token;

                if (obj["type"] is JArray arr)
                {
                    string main = arr
                        .Select(v => v.ToString())
                        .FirstOrDefault(v => v != "null") ?? "string";

                    obj["type"] = main;
                }

                foreach (var p in obj.Properties())
                    NormalizeNullableTypes(p.Value);
            }
            else if (token.Type == JTokenType.Array)
            {
                foreach (var child in token.Children())
                    NormalizeNullableTypes(child);
            }
        }

        // ----------------------------------------------------------
        // Add required[] to all objects
        // ----------------------------------------------------------
        private static void AddRequiredRecursive(JToken token)
        {
            if (token.Type == JTokenType.Object)
            {
                var obj = (JObject)token;

                if (obj["type"]?.ToString() == "object" &&
                    obj["properties"] is JObject props)
                {
                    obj["required"] = new JArray(props.Properties().Select(p => p.Name));
                }

                foreach (var p in obj.Properties())
                    AddRequiredRecursive(p.Value);
            }
            else if (token.Type == JTokenType.Array)
            {
                foreach (var child in token.Children())
                    AddRequiredRecursive(child);
            }
        }

        // ----------------------------------------------------------
        // Force additionalProperties:false on all objects
        // ----------------------------------------------------------
        private static void EnforceAdditionalPropertiesFalse(JToken token)
        {
            if (token.Type == JTokenType.Object)
            {
                var obj = (JObject)token;

                if (obj["type"]?.ToString() == "object")
                {
                    if (obj["additionalProperties"] == null)
                        obj["additionalProperties"] = false;

                    if (obj["properties"] == null)
                        obj["properties"] = new JObject();

                    if (obj["required"] == null)
                        obj["required"] = new JArray();
                }

                foreach (var p in obj.Properties())
                    EnforceAdditionalPropertiesFalse(p.Value);
            }
            else if (token.Type == JTokenType.Array)
            {
                foreach (var child in token.Children())
                    EnforceAdditionalPropertiesFalse(child);
            }
        }
    }
}
