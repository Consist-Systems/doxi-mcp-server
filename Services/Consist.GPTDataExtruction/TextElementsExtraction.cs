using Consist.GPTDataExtruction.Model;
using Newtonsoft.Json;

namespace Consist.GPTDataExtruction
{
    public class TextElementsExtraction
    {
        private readonly GenericGptClient _genericGptClient;

        private const string PROMPT_ANELIZE_TEXT_ELEMENT_INSTRUCTIONS = @"You are an AI document command parser.

Your task is to analyze the user's full natural-language instruction and extract
all requested text insertions as discrete instructions.

You must:
- Identify every distinct text that the user wants to add to the document
- Determine the intent / command describing WHERE or HOW that text should be placed
- Determine whether the text is intended to be placed inside an existing document field
  (such as a form field like Name, Date, ID, etc.)

Rules:
1. Do NOT calculate coordinates, fonts, sizes, pages, or layout.
2. Do NOT guess document structure.
3. Do NOT merge multiple instructions into one.
4. Each output object must represent exactly one text insertion request.
5. If the user refers to filling a known form field (e.g., ""name"", ""date"", ""ID""),
   mark IsInField = true.
6. If the text is a header, title, footer, or free document text,
   mark IsInField = false.
7. Command must be a short imperative phrase describing the action,
   e.g.:
   - ""Fill the name field""
   - ""Add document header""
   - ""Add footer note""
   - ""Insert payment note near payment section""
8. Write the field name in the same language that it write in the prompt.
9. Text must contain ONLY the exact text to be inserted, without explanation.

Output:
- Return ONLY a valid JSON array.
- Do NOT include markdown, comments, or additional text.
User instruction:
";

        private const string GET_TEXT_ELEMENTS_INSTRUCTIONS = @"**GPT API Prompt (minimal):**

Input:
- A JSON containing an instruction and document elements linked to it.
- Image list of the PDF - Image per page
- documentStructure JSON: contain the full document structure - ReferenceElement taked from this JSON.

Task:
Place the given text (`Text`) onto the PDF at the most appropriate position relative to the `ReferenceElement`.

Rules:

* `PageNumber` specifies the PDF page to modify.
* Determine a position (`X`, `Y`) in points relative to the bounding box of `ReferenceElement`.
* Use the same font and font size as the `ReferenceElement`.
Input JSON:";

        private const string TEXT_ELEMENTS_METADATA_INSTRUCTIONS = @"Provide:

Image files, where each image is a single page from the same PDF.

A JSON (documentStructure) describing elements and their positions per page.

Input JSON contains instructions.
For each instruction, return the element(s) from the images/JSON with the highest logical match.
Return multiple elements if applicable.
ReferenceElement must contain all the element data - type, position and font - copy the element from the documentStructure JSON
PageNumber is the page that the ReferenceElement is on.

Example:
Instruction: “Enter first name” ? return all text fields labeled “First Name:” across all pages.

JSON Commands Input:";

        public TextElementsExtraction(GenericGptClient genericGptClient)
        {
            _genericGptClient = genericGptClient;
        }
        internal async Task<IEnumerable<TextElement>> GetTextElements(IEnumerable<byte[]> documentPagesAsImages, string documentFields, string documentStructure, string prompt)
        {
            //Step 1: extruct instruction and text from the prompt
            var textElementsExtractions = await _genericGptClient.RunModelByText<TextElementInstructionsRoot>(string.Concat(PROMPT_ANELIZE_TEXT_ELEMENT_INSTRUCTIONS,prompt));

            //Step 2: Get page , Is multiple and releted objects
            var requestFiles = new List<byte[]>(documentPagesAsImages);
            documentStructure = $"documentStructure:{documentStructure}";
            var commands = JsonConvert.SerializeObject(textElementsExtractions.TextElementInstructions.Select(x => x.Command));
            var textElementsMetadataInstructions = string.Concat(TEXT_ELEMENTS_METADATA_INSTRUCTIONS, commands);
            var textElementsMetadataRoot = await _genericGptClient.RunModelByFiles<TextCommandRelatedObjectsRoot>(requestFiles,
                new[] { documentStructure }
                , textElementsMetadataInstructions);


            var textCommandRelatedObjectsWithText = MapTextCommandRelatedObjectsWithText(textElementsExtractions.TextElementInstructions,textElementsMetadataRoot.TextCommandsRelatedObjects);
            var getTextElementsInstructions = string.Concat(GET_TEXT_ELEMENTS_INSTRUCTIONS, JsonConvert.SerializeObject(textCommandRelatedObjectsWithText));
            var result = await _genericGptClient.RunModelByFiles<RootTextElement>(requestFiles,
                new[] { documentStructure }
                , getTextElementsInstructions);
            
            return result.TextElementArray;

        }

        private IEnumerable<TextCommandRelatedObjectsWithText> MapTextCommandRelatedObjectsWithText(TextElementInstructions[] textElementInstructions, TextCommandRelatedObjects[] textCommandsRelatedObjects)
        {
            foreach (var textCommandRelatedObjects in textCommandsRelatedObjects)
            {
                var textElementInstruction = textElementInstructions.First(x=>x.Command == textCommandRelatedObjects.Command);
                TextCommandRelatedObjectsWithText result = null;
                try
                {
                    result = new TextCommandRelatedObjectsWithText
                    {
                        Command = textCommandRelatedObjects.Command,
                        Text = textElementInstruction.Text,
                        TextCommandRelatedObject = textCommandRelatedObjects.TextCommandRelatedObject.Select(textCommandRelatedObject =>
                            new TextCommandRelatedObject<dynamic>
                            {
                                PageNumber = textCommandRelatedObject.PageNumber,
                                ReferenceElement = JsonConvert.DeserializeObject(textCommandRelatedObject.ReferenceElement)
                            }).ToArray()
                    };
                }
                catch(Exception ex)
                {
                    //Do nothing
                }
                yield return result;
            }
        }
    }
}
