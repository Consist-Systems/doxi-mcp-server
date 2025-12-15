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
8. Text must contain ONLY the exact text to be inserted, without explanation.

Output:
- Return ONLY a valid JSON array.
- Do NOT include markdown, comments, or additional text.
User instruction:
";

        private const string GET_TEXT_ELEMENTS_INSTRUCTIONS = @"You are a PDF text insertion planner and resolver.

You must perform your task in TWO STRICT PHASES:

PHASE 1 – INSTRUCTION EXTRACTION
--------------------------------
From the user’s natural-language instruction, you must extract a logical
TextElementInstructionsRoot object.

TextElementInstructionsRoot contains an array of TextElementInstructions,
where each instruction represents ONE atomic text insertion request.

Each TextElementInstructions must contain:
- Text: the exact text to insert
- Command: a short imperative command describing WHAT to do
- IsInField: true if the text is intended to be placed inside an existing form field,
             false if it is free document text (header, title, note, etc.)

Rules for PHASE 1:
- Do NOT determine font, font size, coordinates, or page numbers.
- Do NOT merge multiple text insertions into one instruction.
- Commands must be explicit and human-readable.
- Field-related instructions (e.g., name, date, ID) must have IsInField = true.
- Headers, titles, footers, or notes must have IsInField = false.

PHASE 2 – INSTRUCTION RESOLUTION
--------------------------------
Using:
- documentStructure.json (Apryse document structure)
- DocumentFields.json (Apryse form fields)
- PDF page images (visual validation only)

You must resolve each TextElementInstruction into a concrete TextElement.

Rules for PHASE 2:
- Fonts and font sizes MUST be copied from existing nearby text elements
  found in documentStructure.json.
- Coordinates (X, Y, Width, Height) MUST be derived from existing Rect values
  found in documentStructure.json or DocumentFields.json.
- PageNumber MUST come from the resolved anchor element.
- You MUST NOT invent fonts, sizes, pages, or positions.
- If an instruction cannot be resolved deterministically, omit it.

FIELD RESOLUTION RULES
----------------------
For instructions with IsInField = true:
- Locate the appropriate formTextField in DocumentFields.json.
- Locate the nearest descriptive label (e.g., ""Name:"", ""Date:"") in documentStructure.json.
- Copy Font and FontSize from the label text.
- Place the text inside the form field rectangle.
- Use the field’s page number.

FREE TEXT RESOLUTION RULES
--------------------------
For instructions with IsInField = false:
- For document headers or titles:
  - Use page 1.
  - Use the dominant font family on page 1.
  - Font size must be larger than body text.
  - Horizontally center the text.
  - Place near the top margin.
- For other free text:
  - Anchor near the referenced section if possible.
  - Otherwise, omit the instruction.

OUTPUT RULES
------------
- You must output ONLY the final resolved TextElement array.
- Do NOT output TextElementInstructionsRoot.
- Do NOT explain or describe your reasoning.
- Do NOT include markdown or comments.

TextElementInstructionsRoot :
";

        public TextElementsExtraction(GenericGptClient genericGptClient)
        {
            _genericGptClient = genericGptClient;
        }
        internal async Task<IEnumerable<TextElement>> GetTextElements(IEnumerable<byte[]> documentPagesAsImages, string documentFields, string documentStructure, string prompt)
        {

            var textElementsExtractions = await _genericGptClient.RunModelByText<TextElementInstructionsRoot>(string.Concat(PROMPT_ANELIZE_TEXT_ELEMENT_INSTRUCTIONS,prompt));

            var requestFiles = new List<byte[]>(documentPagesAsImages);
           
            var getTextElementsInstructions = string.Concat(GET_TEXT_ELEMENTS_INSTRUCTIONS, JsonConvert.SerializeObject(textElementsExtractions.TextElementInstructions));
            var result = await _genericGptClient.RunModelByFiles<RootTextElement>(requestFiles,
                new[] { documentFields , documentStructure }
                , getTextElementsInstructions);
            
            return result.TextElementArray;

        }

        
    }
}
