using Consist.GPTDataExtruction;
using Consist.GPTDataExtruction.Model;
using Flurl.Http;
using Flurl.Http.Testing;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Consist.GPTDataExtruction.Tests
{
    [TestFixture]
    public class ExtractTemplateInformationTests
    {
        private GPTDataExtractionClient _client;
        private GPTDataExtructionConfiguration _config;

        [SetUp]
        public void Setup()
        {
            _config = new GPTDataExtructionConfiguration
            {
                GPTAPIKey = "test-api-key",
                ExtractTemplateInformationModel = "ft:gpt-4.1-nano-2025-04-14:personal:doxicreatetemplateinformation:CcsWe2l4"
            };

            _client = new GPTDataExtractionClient(_config);
        }


        [Test]
        public async Task ExtractTemplateInformation_WithHebrewInstructions_ReturnsCorrectTemplateInformation()
        {
            // Arrange
            var templateInstructions = @"יוצר:sender@consist.co.il
תבנית הזמנת רכש,

חותם ראשון לקוח, חותם שני מנהל הכספים ronenr@consist.co.il";

            // Expected response structure from OpenAI API
            var mockResponse = new
            {
                choices = new[]
                {
                    new
                    {
                        message = new
                        {
                            content = JsonConvert.SerializeObject(new CreateTemplateInformation
                            {
                                Name = "תבנית הזמנת רכש",
                                SenderEmail = "sender@consist.co.il",
                                SendMethodType = 0,
                                Signers = new[]
                                {
                                    new SignerInfo
                                    {
                                        Title = "לקוח",
                                        Index = 0,
                                        SignerType = 0,
                                        FixedSigner = null
                                    },
                                    new SignerInfo
                                    {
                                        Title = "מנהל הכספים",
                                        Index = 1,
                                        SignerType = 1,
                                        FixedSigner = new FixedSigner
                                        {
                                            Email = "ronenr@consist.co.il",
                                            PhoneNumber = null,
                                            FirstName = null,
                                            LastName = null
                                        }
                                    }
                                }
                            })
                        }
                    }
                }
            };

            var responseJson = JsonConvert.SerializeObject(mockResponse);


            // Act
            var result = await _client.ExtractTemplateInformation(templateInstructions);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("תבנית הזמנת רכש"));
            Assert.That(result.SenderEmail, Is.EqualTo("sender@consist.co.il"));
            Assert.That(result.Signers, Is.Not.Null);
            Assert.That(result.Signers.Count(), Is.EqualTo(2));

            var firstSigner = result.Signers.First();
            Assert.That(firstSigner.Title, Is.EqualTo("לקוח"));
            Assert.That(firstSigner.Index, Is.EqualTo(0));
            Assert.That(firstSigner.SignerType, Is.EqualTo(0));

            var secondSigner = result.Signers.Skip(1).First();
            Assert.That(secondSigner.Title, Is.EqualTo("מנהל הכספים"));
            Assert.That(secondSigner.Index, Is.EqualTo(1));
            Assert.That(secondSigner.SignerType, Is.EqualTo(1));
            Assert.That(secondSigner.FixedSigner, Is.Not.Null);
            Assert.That(secondSigner.FixedSigner.Email, Is.EqualTo("ronenr@consist.co.il"));

        }

      

        [Test]
        public async Task ExtractTemplateInformation_WithPurchaseOrderTemplate_ExtractsCorrectInformation()
        {
            // Arrange - Exact template instructions as provided by user
            var templateInstructions = @"תבנית הזמנת רכש,

חותם ראשון לקוח, חותם שני מנהל הכספים ronenr@consist.co.il";

            // Expected response structure from OpenAI API
            var expectedResult = new CreateTemplateInformation
            {
                Name = "תבנית הזמנת רכש",
                SenderEmail = "ronenr@consist.co.il",
                SendMethodType = 0,
                Signers = new[]
                {
                    new SignerInfo
                    {
                        Title = "לקוח",
                        Index = 0,
                        SignerType = 0,
                        FixedSigner = null
                    },
                    new SignerInfo
                    {
                        Title = "מנהל הכספים",
                        Index = 1,
                        SignerType = 0,
                        FixedSigner = new FixedSigner
                        {
                            Email = "ronenr@consist.co.il",
                            PhoneNumber = null,
                            FirstName = null,
                            LastName = null
                        }
                    }
                }
            };

            var mockResponse = new
            {
                choices = new[]
                {
                    new
                    {
                        message = new
                        {
                            content = JsonConvert.SerializeObject(expectedResult)
                        }
                    }
                }
            };

            var responseJson = JsonConvert.SerializeObject(mockResponse);


            // Act
            var result = await _client.ExtractTemplateInformation(templateInstructions);

            // Assert
            Assert.That(result, Is.Not.Null, "Result should not be null");
            Assert.That(result.Name, Is.EqualTo("תבנית הזמנת רכש"), "Template name should match");
            Assert.That(result.SenderEmail, Is.EqualTo("ronenr@consist.co.il"), "Sender email should be extracted correctly");
            Assert.That(result.SendMethodType, Is.EqualTo(0), "SendMethodType should be set");
            
            // Validate signers
            Assert.That(result.Signers, Is.Not.Null, "Signers should not be null");
            Assert.That(result.Signers.Count(), Is.EqualTo(2), "Should have exactly 2 signers");

            // Validate first signer (לקוח - Customer)
            var firstSigner = result.Signers.First();
            Assert.That(firstSigner.Title, Is.EqualTo("לקוח"), "First signer title should be 'לקוח'");
            Assert.That(firstSigner.Index, Is.EqualTo(0), "First signer index should be 0");
            Assert.That(firstSigner.FixedSigner, Is.Null, "First signer should not have fixed signer details");

            // Validate second signer (מנהל הכספים - CFO)
            var secondSigner = result.Signers.Skip(1).First();
            Assert.That(secondSigner.Title, Is.EqualTo("מנהל הכספים"), "Second signer title should be 'מנהל הכספים'");
            Assert.That(secondSigner.Index, Is.EqualTo(1), "Second signer index should be 1");
            Assert.That(secondSigner.FixedSigner, Is.Not.Null, "Second signer should have fixed signer details");
            Assert.That(secondSigner.FixedSigner.Email, Is.EqualTo("ronenr@consist.co.il"), "Second signer email should match");


            
        }
    }
}

