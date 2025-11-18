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
            // Step 1 — Generate raw schema with definitions
            var raw = JsonSchema.FromType<T>();
            JObject root = JObject.Parse(raw.ToJson());

            // Step 2 — Force required OpenAI fields
            root["title"] = typeof(T).Name;
            root["type"] = "object";

            // Step 3 — Flatten all $ref references BEFORE removing definitions
            if (root["definitions"] is JObject defs)
                FlattenAllRefs(root, defs);

            // Step 4 — Remove definitions
            root.Remove("definitions");

            // Step 5 — Clean forbidden constructs
            RemoveOneOf(root);
            NormalizeNullableTypes(root);

            // Step 6 — Add required[] to all objects
            AddRequiredRecursive(root);

            // Step 7 — Ensure additionalProperties=false everywhere
            EnforceAdditionalPropertiesFalse(root);

            return JsonNode.Parse(root.ToString());
        }

        // --------------------------------------------------------------------
        //  FLATTEN ALL $ref REFERENCES
        // --------------------------------------------------------------------
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

        private static void RemoveOneOf(JToken token)
        {
            if (token.Type == JTokenType.Object)
            {
                var obj = (JObject)token;

                if (obj["oneOf"] is JArray arr)
                {
                    // Try to pick the "real" object schema from oneOf
                    var concrete = arr
                        .OfType<JObject>()
                        .FirstOrDefault(o =>
                            o["type"]?.ToString() == "object" ||
                            o["properties"] is JObject);

                    if (concrete != null)
                    {
                        // Replace current object contents with the concrete schema
                        obj.Remove("oneOf");

                        foreach (var prop in concrete.Properties())
                        {
                            obj[prop.Name] = prop.Value.DeepClone();
                        }
                    }
                    else
                    {
                        // Fallback: no usable object found, just make it a generic object
                        obj.Remove("oneOf");
                        obj["type"] = "object";
                    }
                }

                // Recurse into children
                foreach (var p in obj.Properties())
                    RemoveOneOf(p.Value);
            }
            else if (token.Type == JTokenType.Array)
            {
                foreach (var child in token.Children())
                    RemoveOneOf(child);
            }
        }


        // --------------------------------------------------------------------
        // FIX nullable union types like ["string","null"]
        // --------------------------------------------------------------------
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

        // --------------------------------------------------------------------
        // ADD required[] FOR ALL OBJECTS
        // --------------------------------------------------------------------
        private static void AddRequiredRecursive(JToken token)
        {
            if (token.Type == JTokenType.Object)
            {
                var obj = (JObject)token;

                if (obj["type"]?.ToString() == "object" && obj["properties"] is JObject props)
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

        // --------------------------------------------------------------------
        // FORCE additionalProperties:false FOR ALL OBJECTS
        // --------------------------------------------------------------------
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
